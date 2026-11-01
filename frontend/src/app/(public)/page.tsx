// @Role: Strona główna / z tekstami z komunikatów | @DependsOn: next-intl/server, messages/pl-PL.json | @UsedBy: Next.js App Router (trasa /) | @Task: TASK-006
import { getTranslations } from "next-intl/server";

export default async function StronaGlowna() {
  const t = await getTranslations("Strona");
  return (
    <main className="mx-auto max-w-2xl p-6">
      <h1 className="text-3xl font-bold">{t("naglowek")}</h1>
      <p className="mt-4 text-lg">{t("opis")}</p>
    </main>
  );
}
