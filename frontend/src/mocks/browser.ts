// @Role: Worker MSW dla przeglądarki (tryb NEXT_PUBLIC_LEMAT_API=mock); ładowany wyłącznie dynamicznie przez MockProvider | @DependsOn: msw/browser, mocks/handlers | @UsedBy: mocks/MockProvider | @Task: TASK-132
import { setupWorker } from "msw/browser";
import { handlers } from "./handlers";

export const worker = setupWorker(...handlers);

/** Uruchamia Service Worker (public/mockServiceWorker.js); nieobsłużone żądania (statyki Next.js) przechodzą bez zmian. */
export async function uruchomMock(): Promise<void> {
  await worker.start({
    onUnhandledRequest: "bypass",
    serviceWorker: { url: "/mockServiceWorker.js" },
    quiet: true,
  });
}
