// @Role: Worker MSW dla przeglądarki (tryb NEXT_PUBLIC_LEMAT_API=mock); ładowany wyłącznie dynamicznie przez MockProvider | @DependsOn: msw/browser, mocks/handlers | @UsedBy: mocks/MockProvider | @Task: TASK-132, TASK-139
import { setupWorker } from "msw/browser";
import { handlers } from "./handlers";

export const worker = setupWorker(...handlers);

let uruchomienie: Promise<void> | undefined;

/** Uruchamia Service Worker (public/mockServiceWorker.js); nieobsłużone żądania (statyki Next.js) przechodzą bez zmian. */
export function uruchomMock(): Promise<void> {
  // Jedno uruchomienie na kartę: React Strict Mode (dev) wywołuje efekt dwa razy, a drugi start() rzuca wyjątek.
  uruchomienie ??= worker
    .start({
      onUnhandledRequest: "bypass",
      serviceWorker: { url: "/mockServiceWorker.js" },
      quiet: true,
    })
    .then(() => undefined);
  return uruchomienie;
}
