// @Role: Prymitywy formularza (Formularz, PoleFormularza) z etykietą i komunikatem błędu | @DependsOn: react, clsx | @UsedBy: ekrany wszystkich ról (planowane) | @Task: TASK-006
import clsx from "clsx";
import { useId, type FormHTMLAttributes, type InputHTMLAttributes } from "react";

export function Form({ className, ...reszta }: FormHTMLAttributes<HTMLFormElement>) {
  return <form className={clsx("flex flex-col gap-4", className)} noValidate {...reszta} />;
}

export interface FormFieldProps extends InputHTMLAttributes<HTMLInputElement> {
  etykieta: string;
  blad?: string;
}

export function FormField({ etykieta, blad, id, className, ...reszta }: FormFieldProps) {
  const wygenerowaneId = useId();
  const poleId = id ?? wygenerowaneId;
  const bladId = `${poleId}-blad`;
  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={poleId} className="font-medium">
        {etykieta}
      </label>
      <input
        id={poleId}
        aria-invalid={blad ? true : undefined}
        aria-describedby={blad ? bladId : undefined}
        className={clsx("min-h-11 rounded-md border border-slate-300 px-3 py-2", className)}
        {...reszta}
      />
      {blad ? (
        <p id={bladId} role="alert" className="text-sm text-red-700">
          {blad}
        </p>
      ) : null}
    </div>
  );
}
