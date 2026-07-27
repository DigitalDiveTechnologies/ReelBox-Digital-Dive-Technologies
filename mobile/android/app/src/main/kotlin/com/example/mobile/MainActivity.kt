package com.example.mobile

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.util.Log
import androidx.core.content.FileProvider
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import java.io.BufferedReader
import java.io.File
import java.io.InputStreamReader

/**
 * Hosts Flutter and forwards Android ACTION_SEND share intents
 * into the Dart share feature via a MethodChannel.
 */
class MainActivity : FlutterActivity() {
    companion object {
        private const val TAG = "SocialShareIntent"
        private const val CHANNEL = "com.example.mobile/share_intent"
        private const val METHOD_GET_INITIAL = "getInitialSharedText"
        private const val METHOD_ON_SHARED = "onSharedText"
        private const val METHOD_SHARE_FILE = "shareFile"

        /** Survives activity recreation until Flutter consumes the payload. */
        @Volatile
        var pendingShareText: String? = null
    }

    private var methodChannel: MethodChannel? = null
    private var flutterEngineReady = false
    private var lastDeliveredShareText: String? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        logIntent("onCreate", intent)
        captureShareFromIntent(intent)
        super.onCreate(savedInstanceState)
    }

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        captureShareFromIntent(intent)

        methodChannel = MethodChannel(
            flutterEngine.dartExecutor.binaryMessenger,
            CHANNEL,
        ).also { channel ->
            channel.setMethodCallHandler { call, result ->
                when (call.method) {
                    METHOD_GET_INITIAL -> {
                        val text = pendingShareText
                        Log.i(
                            TAG,
                            "getInitialSharedText → ${if (text.isNullOrBlank()) "null" else "len=${text.length}"}",
                        )
                        result.success(text)
                        pendingShareText = null
                        if (!text.isNullOrBlank()) {
                            lastDeliveredShareText = text
                            clearShareExtras(intent)
                        }
                    }
                    METHOD_SHARE_FILE -> {
                        @Suppress("UNCHECKED_CAST")
                        val args = call.arguments as? Map<String, Any?>
                        val path = args?.get("path") as? String
                        val mimeType = (args?.get("mimeType") as? String) ?: "video/mp4"
                        val text = args?.get("text") as? String
                        if (path.isNullOrBlank()) {
                            result.error("invalid_args", "Missing file path.", null)
                            return@setMethodCallHandler
                        }
                        try {
                            shareFile(path, mimeType, text)
                            result.success(true)
                        } catch (ex: Exception) {
                            result.error("share_failed", ex.message, null)
                        }
                    }
                    else -> result.notImplemented()
                }
            }
        }
        flutterEngineReady = true
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        logIntent("onNewIntent", intent)

        captureShareFromIntent(intent)
        flushPendingShareToFlutter("onNewIntent")
    }

    private fun captureShareFromIntent(intent: Intent?) {
        val text = extractSharedText(intent)
        if (text.isNullOrBlank()) {
            return
        }
        pendingShareText = text
        Log.i(TAG, "Captured share payload (len=${text.length})")
    }

    private fun flushPendingShareToFlutter(source: String) {
        val text = pendingShareText ?: return
        if (text == lastDeliveredShareText) {
            return
        }
        val channel = methodChannel
        if (!flutterEngineReady || channel == null) {
            Log.d(TAG, "Defer Flutter delivery ($source): engine not ready")
            return
        }

        runOnUiThread {
            Log.i(TAG, "Deliver onSharedText ($source, len=${text.length})")
            channel.invokeMethod(
                METHOD_ON_SHARED,
                text,
                object : MethodChannel.Result {
                    override fun success(result: Any?) {
                        pendingShareText = null
                        lastDeliveredShareText = text
                        clearShareExtras(intent)
                    }

                    override fun error(
                        errorCode: String,
                        errorMessage: String?,
                        errorDetails: Any?,
                    ) {
                        Log.w(TAG, "onSharedText error: $errorCode $errorMessage")
                    }

                    override fun notImplemented() {
                        Log.w(TAG, "onSharedText notImplemented — Dart handler missing?")
                    }
                },
            )
        }
    }

    private fun shareFile(path: String, mimeType: String, text: String?) {
        val file = File(path)
        if (!file.exists()) {
            throw IllegalArgumentException("File does not exist: $path")
        }

        val uri = FileProvider.getUriForFile(
            this,
            "${applicationContext.packageName}.fileprovider",
            file,
        )

        val send = Intent(Intent.ACTION_SEND).apply {
            type = mimeType
            putExtra(Intent.EXTRA_STREAM, uri)
            if (!text.isNullOrBlank()) {
                putExtra(Intent.EXTRA_TEXT, text)
            }
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }

        startActivity(Intent.createChooser(send, "Share media"))
    }

    private fun extractSharedText(intent: Intent?): String? {
        if (intent == null) return null
        val action = intent.action ?: return null
        if (action != Intent.ACTION_SEND && action != Intent.ACTION_SEND_MULTIPLE) {
            return null
        }

        intent.getStringExtra(Intent.EXTRA_TEXT)
            ?.trim()
            ?.takeIf { it.isNotEmpty() }
            ?.let { return it }

        intent.getCharSequenceExtra(Intent.EXTRA_TEXT)
            ?.toString()
            ?.trim()
            ?.takeIf { it.isNotEmpty() }
            ?.let { return it }

        intent.getStringExtra(Intent.EXTRA_SUBJECT)
            ?.trim()
            ?.takeIf { it.isNotEmpty() }
            ?.let { return it }

        intent.data
            ?.toString()
            ?.trim()
            ?.takeIf { it.startsWith("http://") || it.startsWith("https://") }
            ?.let { return it }

        @Suppress("DEPRECATION")
        val streamUri: Uri? = intent.getParcelableExtra(Intent.EXTRA_STREAM)
        streamUri?.let { uri ->
            uriToShareText(uri)?.let { return it }
        }

        val clip = intent.clipData
        if (clip != null) {
            for (i in 0 until clip.itemCount) {
                val item = clip.getItemAt(i) ?: continue
                item.text?.toString()?.trim()?.takeIf { it.isNotEmpty() }?.let { return it }
                item.uri?.let { uri ->
                    uriToShareText(uri)?.let { return it }
                }
            }
        }

        if (action == Intent.ACTION_SEND_MULTIPLE) {
            @Suppress("DEPRECATION")
            val streams = intent.getParcelableArrayListExtra<Uri>(Intent.EXTRA_STREAM)
            streams?.forEach { uri ->
                uriToShareText(uri)?.let { return it }
            }
        }

        return null
    }

    private fun uriToShareText(uri: Uri): String? {
        val raw = uri.toString().trim()
        if (raw.startsWith("http://") || raw.startsWith("https://")) {
            return raw
        }
        if (uri.scheme != "content") {
            return null
        }
        return try {
            contentResolver.getType(uri)?.let { type ->
                if (!type.startsWith("text")) return@let null
            }
            contentResolver.openInputStream(uri)?.use { input ->
                BufferedReader(InputStreamReader(input)).readText().trim()
                    .takeIf { it.isNotEmpty() }
            }
        } catch (ex: Exception) {
            Log.d(TAG, "Could not read content URI as text: $uri (${ex.message})")
            null
        }
    }

    private fun clearShareExtras(intent: Intent?) {
        if (intent == null) return
        val action = intent.action
        if (action != Intent.ACTION_SEND && action != Intent.ACTION_SEND_MULTIPLE) {
            return
        }
        intent.removeExtra(Intent.EXTRA_TEXT)
        intent.removeExtra(Intent.EXTRA_SUBJECT)
        intent.removeExtra(Intent.EXTRA_STREAM)
        intent.clipData = null
        intent.data = null
        intent.action = Intent.ACTION_MAIN
    }

    private fun logIntent(source: String, intent: Intent?) {
        if (intent == null) {
            Log.d(TAG, "$source: intent=null")
            return
        }
        Log.d(
            TAG,
            "$source: action=${intent.action} type=${intent.type} " +
                "data=${intent.dataString} extras=${intent.extras?.keySet()?.joinToString()}",
        )
    }
}
