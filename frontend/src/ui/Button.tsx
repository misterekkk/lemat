// @Role: Podstawowy przycisk (Przycisk) z wariantami Tailwind | @DependsOn: react, clsx | @UsedBy: ekrany wszystkich ról (planowane) | @Task: TASK-006
import clsx from "clsx";
import type { ButtonHTMLAttributes } from "react";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  wariant?: "podstawowy" | "drugorzedny";
}

export function Button({
  wariant = "podstawowy",
  className,
  type = "button",
  ...reszta
}: ButtonProps) {
  return (
    <button
      type={type}
      className={clsx(
        "inline-flex min-h-11 items-center justify-center rounded-md px-4 py-2 font-medium focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600 disabled:opacity-50",
        wariant === "podstawowy" && "bg-blue-700 text-white hover:bg-blue-800",
        wariant === "drugorzedny" &&
          "border border-slate-300 bg-white text-slate-900 hover:bg-slate-50",
        className,
      )}
      {...reszta}
    />
  );
}
