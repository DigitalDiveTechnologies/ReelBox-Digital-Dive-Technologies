import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="page">
      <p class="code">404</p>
      <h1>Page not found</h1>
      <p class="msg">This admin route does not exist.</p>
      <a routerLink="/dashboard" class="link">Back to dashboard</a>
    </section>
  `,
  styles: [
    `
      .page {
        min-height: 60vh;
        display: grid;
        place-content: center;
        text-align: center;
        gap: 0.5rem;
        padding: 2rem;
      }
      .code {
        margin: 0;
        font-size: 3rem;
        font-weight: 700;
        color: var(--brand);
      }
      h1 {
        margin: 0;
        font-size: 1.5rem;
      }
      .msg {
        margin: 0;
        color: var(--text-muted);
      }
      .link {
        margin-top: 1rem;
        color: var(--brand);
        font-weight: 600;
      }
    `,
  ],
})
export class NotFoundPageComponent {}
