// @Role: Konfiguracja next-intl po stronie serwera: jedna lokalizacja pl-PL, strefa Europe/Warsaw | @DependsOn: next-intl/server, messages/pl-PL.json | @UsedBy: next.config.ts (plugin next-intl) | @Task: TASK-006
import { getRequestConfig } from "next-intl/server";

export default getRequestConfig(async () => ({
  locale: "pl-PL",
  timeZone: "Europe/Warsaw",
  messages: (await import("../../../messages/pl-PL.json")).default,
}));
