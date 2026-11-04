// @Role: Strona 404 po polsku z linkiem na stronę główną | @DependsOn: next-intl | @UsedBy: Next.js App Router | @Task: TASK-138
import { useTranslations } from "next-intl";
import Link from "next/link";

export default function NieZnaleziono() {
  const t = useTranslations("Stany");
  return (
    <main className="mx-auto flex max-w-md flex-col gap-3 p-6">
      <h1 className="text-2xl font-bold">{t("brakStronyTytul")}</h1>
      <p>{t("brakStronyOpis")}</p>
      <Link href="/" className="inline-flex min-h-11 items-center text-blue-700 underline">
        {t("wrocNaStrone")}
      </Link>
    </main>
  );
}
