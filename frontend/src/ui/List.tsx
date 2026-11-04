// @Role: Lista (Lista) dostępna semantycznie; komunikat dla pustej listy przekazywany z zewnątrz | @DependsOn: react, clsx | @UsedBy: ekrany wszystkich ról (planowane) | @Task: TASK-006
import clsx from "clsx";
import type { ReactNode } from "react";

export interface ListProps<T> {
  elementy: readonly T[];
  kluczElementu: (element: T) => string;
  renderElement: (element: T) => ReactNode;
  komunikatPusta: string;
  className?: string;
}

export function List<T>({
  elementy,
  kluczElementu,
  renderElement,
  komunikatPusta,
  className,
}: ListProps<T>) {
  if (elementy.length === 0) {
    return <p className="text-slate-600">{komunikatPusta}</p>;
  }
  return (
    <ul className={clsx("divide-y divide-slate-200", className)}>
      {elementy.map((element) => (
        <li key={kluczElementu(element)} className="py-3">
          {renderElement(element)}
        </li>
      ))}
    </ul>
  );
}
