// @Role: Pole wyboru (lista rozwijana) z etykietą i komunikatem błędu, spójne z FormField | @DependsOn: react, clsx | @UsedBy: features/accounts/SettingsForm | @Task: TASK-134
import clsx from "clsx";
import { forwardRef, useId, type SelectHTMLAttributes } from "react";

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  etykieta: string;
  blad?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { etykieta, blad, id, className, children, ...reszta },
  ref,
) {
  const wygenerowaneId = useId();
  const poleId = id ?? wygenerowaneId;
  const bladId = `${poleId}-blad`;
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={poleId} className="font-medium">
        {etykieta}
      </label>
      <select
        ref={ref}
        id={poleId}
        aria-invalid={blad ? true : undefined}
        aria-describedby={blad ? bladId : undefined}
        className={clsx(
          "min-h-11 rounded-md border border-slate-300 bg-white px-3 py-2",
          className,
        )}
        {...reszta}
      >
        {children}
      </select>
      {blad ? (
        <p id={bladId} role="alert" className="text-sm text-red-700">
          {blad}
        </p>
      ) : null}
    </div>
  );
});
