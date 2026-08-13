package com.example.mobile

import android.content.ClipData
import android.content.ContentUris
import android.content.ContentValues
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.Environment
import android.provider.MediaStore
import android.util.Log
import androidx.core.content.FileProvider
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import java.io.BufferedReader
import java.io.File
import java.io.FileInputStream
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
        private const val METHOD_GET_CACHE_DIR = "getCacheDir"
        private const val METHOD_SAVE_VIDEO_TO_GALLERY = "saveVideoToGallery"
        private const val METHOD_SHARE_GALLERY_BY_MEDIA_ID = "shareGalleryVideoByMediaId"

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
                    METHOD_GET_CACHE_DIR -> {
                        result.success(cacheDir.absolutePath)
                    }
                    METHOD_SHARE_GALLERY_BY_MEDIA_ID -> {
                        @Suppress("UNCHECKED_CAST")
                        val args = call.arguments as? Map<String, Any?>
                        val mediaIdToken = args?.get("mediaIdToken") as? String
                        val mimeType = (args?.get("mimeType") as? String) ?: "video/mp4"
                        val text = args?.get("text") as? String
                        if (mediaIdToken.isNullOrBlank()) {
                            result.error("invalid_args", "Missing mediaIdToken.", null)
                            return@setMethodCallHandler
                        }
                        try {
                            Log.i(TAG, "SHARE_GALLERY: shareGalleryVideoByMediaId token=$mediaIdToken")
                            val shared = shareGalleryVideoByMediaId(
                                mediaIdToken = mediaIdToken,
                                mimeType = mimeType,
                                text = text,
                            )
                            result.success(shared)
                        } catch (ex: Exception) {
                            Log.w(TAG, "shareGalleryVideoByMediaId failed: ${ex.message}")
                            result.error("gallery_share_failed", ex.message, null)
                        }
                    }
                    METHOD_SAVE_VIDEO_TO_GALLERY -> {
                        @Suppress("UNCHECKED_CAST")
                        val args = call.arguments as? Map<String, Any?>
                        val path = args?.get("path") as? String
                        val displayName = args?.get("displayName") as? String
                        val mimeType = (args?.get("mimeType") as? String) ?: "video/mp4"
                        val relativePath =
                            (args?.get("relativePath") as? String) ?: "Movies/ReelBox"
                        if (path.isNullOrBlank() || displayName.isNullOrBlank()) {
                            result.error(
                                "invalid_args",
                                "Missing path or displayName.",
                                null,
                            )
                            return@setMethodCallHandler
                        }
                        // Run blocking MediaStore file I/O on a background thread so the
                        // Android platform thread stays free for concurrent MethodChannel
                        // calls — specifically Task 3's shareGalleryVideoByMediaId queries,
                        // which would otherwise queue behind this handler for 1–8 seconds.
                        Thread {
                            try {
                                val saved = saveVideoToGallery(
                                    path = path,
                                    displayName = displayName,
                                    mimeType = mimeType,
                                    relativePath = relativePath,
                                )
                                runOnUiThread { result.success(saved) }
                            } catch (ex: Exception) {
                                Log.w(TAG, "saveVideoToGallery failed: ${ex.message}")
                                runOnUiThread { result.error("gallery_save_failed", ex.message, null) }
                            }
                        }.start()
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
            clipData = ClipData.newUri(contentResolver, "media", uri)
        }

        startActivity(Intent.createChooser(send, "Share media"))
    }

    /**
     * Shares a ReelBox Gallery/MediaStore video by media-id token without
     * re-downloading from the VPS. Display names are `ReelBox_<token>...`.
     *
     * Returns false when no matching MediaStore row exists (caller may fall back).
     */
    private fun shareGalleryVideoByMediaId(
        mediaIdToken: String,
        mimeType: String,
        text: String?,
    ): Boolean {
        val found = findReelBoxGalleryVideo(mediaIdToken) ?: return false
        shareContentUri(found.first, found.second ?: mimeType, text)
        return true
    }

    private fun findReelBoxGalleryVideo(mediaIdToken: String): Pair<Uri, String?>? {
        val token = mediaIdToken.trim()
        if (token.isEmpty()) return null

        val isQ = Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q
        val collection = if (isQ) {
            MediaStore.Video.Media.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY)
        } else {
            MediaStore.Video.Media.EXTERNAL_CONTENT_URI
        }

        // Include RELATIVE_PATH and IS_PENDING on Q+ for logging and safety checks.
        val projection = if (isQ) arrayOf(
            MediaStore.Video.Media._ID,           // 0
            MediaStore.Video.Media.MIME_TYPE,      // 1
            MediaStore.Video.Media.DISPLAY_NAME,   // 2
            MediaStore.Video.Media.RELATIVE_PATH,  // 3
            MediaStore.Video.Media.IS_PENDING,     // 4
        ) else arrayOf(
            MediaStore.Video.Media._ID,
            MediaStore.Video.Media.MIME_TYPE,
            MediaStore.Video.Media.DISPLAY_NAME,
        )

        // Matches Task 1 names: ReelBox_<token>.mp4 and ReelBox_<token>_<title>.mp4
        // Scoped to Movies/ReelBox on Q+. IS_PENDING=0 is explicit to prevent
        // sharing an in-progress (incomplete) MediaStore entry.
        val nameClause =
            "(${MediaStore.Video.Media.DISPLAY_NAME} LIKE ? OR ${MediaStore.Video.Media.DISPLAY_NAME} LIKE ?)"
        val selection: String
        val args: Array<String>
        if (isQ) {
            selection =
                "$nameClause AND ${MediaStore.Video.Media.RELATIVE_PATH} LIKE ?" +
                " AND ${MediaStore.Video.Media.IS_PENDING}=0"
            args = arrayOf("ReelBox_$token.%", "ReelBox_${token}_%", "%Movies/ReelBox%")
        } else {
            selection = nameClause
            args = arrayOf("ReelBox_$token.%", "ReelBox_${token}_%")
        }

        Log.d(
            TAG,
            "SHARE_GALLERY: query token=$token" +
            " pat1=ReelBox_$token.%" +
            " pat2=ReelBox_${token}_%" +
            " collection=$collection",
        )

        contentResolver.query(
            collection,
            projection,
            selection,
            args,
            "${MediaStore.Video.Media.DATE_ADDED} DESC",
        ).use { cursor ->
            if (cursor == null || !cursor.moveToFirst()) {
                Log.i(TAG, "SHARE_GALLERY: no committed row for token=$token")
                return null
            }
            val id = cursor.getLong(0)
            val mime = cursor.getString(1)
            val displayName = cursor.getString(2)
            val relativePath = if (isQ) cursor.getString(3) else "n/a"
            val isPending = if (isQ) cursor.getInt(4) else 0
            val uri = ContentUris.withAppendedId(collection, id)
            Log.i(
                TAG,
                "SHARE_GALLERY: found id=$id displayName=$displayName" +
                " relativePath=$relativePath IS_PENDING=$isPending uri=$uri",
            )
            return uri to mime
        }
    }

    private fun shareContentUri(uri: Uri, mimeType: String, text: String?) {
        val send = Intent(Intent.ACTION_SEND).apply {
            type = mimeType
            putExtra(Intent.EXTRA_STREAM, uri)
            if (!text.isNullOrBlank()) {
                putExtra(Intent.EXTRA_TEXT, text)
            }
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
            clipData = ClipData.newUri(contentResolver, "media", uri)
        }
        startActivity(Intent.createChooser(send, "Share media"))
    }

    /**
     * Streams a local cache file into MediaStore (Movies/ReelBox) without loading
     * the full video into a byte[]. Skips insert when the same display name already exists.
     */
    private fun saveVideoToGallery(
        path: String,
        displayName: String,
        mimeType: String,
        relativePath: String,
    ): Boolean {
        val file = File(path)
        if (!file.exists() || file.length() <= 0L) {
            throw IllegalArgumentException("Gallery source file missing or empty: $path")
        }

        val normalizedRelative = relativePath.trim().trimEnd('/')
        if (galleryEntryExists(displayName, normalizedRelative)) {
            Log.i(TAG, "Gallery entry already exists: $displayName")
            return true
        }

        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            saveVideoMediaStoreQPlus(file, displayName, mimeType, normalizedRelative)
        } else {
            saveVideoLegacy(file, displayName, normalizedRelative)
        }
    }

    private fun galleryEntryExists(displayName: String, relativePath: String): Boolean {
        val projection = arrayOf(MediaStore.Video.Media._ID)
        val selection: String
        val args: Array<String>
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            selection =
                "${MediaStore.Video.Media.DISPLAY_NAME}=? AND ${MediaStore.Video.Media.RELATIVE_PATH} LIKE ?"
            args = arrayOf(displayName, "%${relativePath.trimEnd('/')}%")
        } else {
            selection = "${MediaStore.Video.Media.DISPLAY_NAME}=?"
            args = arrayOf(displayName)
        }

        contentResolver.query(
            MediaStore.Video.Media.EXTERNAL_CONTENT_URI,
            projection,
            selection,
            args,
            null,
        ).use { cursor ->
            return cursor != null && cursor.moveToFirst()
        }
    }

    private fun saveVideoMediaStoreQPlus(
        file: File,
        displayName: String,
        mimeType: String,
        relativePath: String,
    ): Boolean {
        val values = ContentValues().apply {
            put(MediaStore.Video.Media.DISPLAY_NAME, displayName)
            put(MediaStore.Video.Media.MIME_TYPE, mimeType)
            put(MediaStore.Video.Media.RELATIVE_PATH, relativePath)
            put(MediaStore.Video.Media.IS_PENDING, 1)
        }

        val collection = MediaStore.Video.Media.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY)
        val itemUri = contentResolver.insert(collection, values)
            ?: throw IllegalStateException("MediaStore insert returned null")

        try {
            contentResolver.openOutputStream(itemUri)?.use { output ->
                FileInputStream(file).use { input ->
                    input.copyTo(output, bufferSize = DEFAULT_BUFFER_SIZE)
                }
            } ?: throw IllegalStateException("Could not open MediaStore output stream")

            values.clear()
            values.put(MediaStore.Video.Media.IS_PENDING, 0)
            contentResolver.update(itemUri, values, null, null)
            return true
        } catch (ex: Exception) {
            contentResolver.delete(itemUri, null, null)
            throw ex
        }
    }

    @Suppress("DEPRECATION")
    private fun saveVideoLegacy(
        file: File,
        displayName: String,
        relativePath: String,
    ): Boolean {
        val movies = Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_MOVIES)
        val folderName = relativePath
            .removePrefix("Movies/")
            .removePrefix("Movies")
            .trim('/')
        val targetDir = if (folderName.isEmpty()) {
            movies
        } else {
            File(movies, folderName)
        }
        if (!targetDir.exists() && !targetDir.mkdirs()) {
            throw IllegalStateException("Could not create gallery folder: ${targetDir.absolutePath}")
        }

        val dest = File(targetDir, displayName)
        if (dest.exists()) {
            return true
        }

        FileInputStream(file).use { input ->
            dest.outputStream().use { output ->
                input.copyTo(output, bufferSize = DEFAULT_BUFFER_SIZE)
            }
        }

        // Make the file visible to Gallery apps on API ≤28.
        val intent = Intent(Intent.ACTION_MEDIA_SCANNER_SCAN_FILE)
        intent.data = Uri.fromFile(dest)
        sendBroadcast(intent)
        return true
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
