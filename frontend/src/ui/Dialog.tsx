// @Role: Okno dialogowe oparte na prymitywie Radix UI (dostępne z klawiatury i czytnika ekranu) | @DependsOn: radix-ui, ui/Button | @UsedBy: ekrany wszystkich ról (planowane) | @Task: TASK-006
"use client";

import { Dialog as DialogPrymityw } from "radix-ui";
import type { ReactNode } from "react";
import { Button } from "./Button";

export interface DialogProps {
  otwarty: boolean;
  onZmianaOtwarcia: (otwarty: boolean) => void;
  tytul: string;
  opis?: string;
  etykietaZamkniecia: string;
  children?: ReactNode;
}

export function Dialog({
  otwarty,
  onZmianaOtwarcia,
  tytul,
  opis,
  etykietaZamkniecia,
  children,
}: DialogProps) {
  return (
    <DialogPrymityw.Root open={otwarty} onOpenChange={onZmianaOtwarcia}>
      <DialogPrymityw.Portal>
        <DialogPrymityw.Overlay className="fixed inset-0 bg-black/40" />
        <DialogPrymityw.Content className="fixed top-1/2 left-1/2 w-[92vw] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-lg bg-white p-6 shadow-lg">
          <DialogPrymityw.Title className="text-xl font-semibold">{tytul}</DialogPrymityw.Title>
          {opis ? (
            <DialogPrymityw.Description className="mt-2 text-slate-600">
              {opis}
            </DialogPrymityw.Description>
          ) : (
            <DialogPrymityw.Description className="sr-only">{tytul}</DialogPrymityw.Description>
          )}
          <div className="mt-4">{children}</div>
          <div className="mt-6 flex justify-end">
            <DialogPrymityw.Close asChild>
              <Button wariant="drugorzedny">{etykietaZamkniecia}</Button>
            </DialogPrymityw.Close>
          </div>
        </DialogPrymityw.Content>
      </DialogPrymityw.Portal>
    </DialogPrymityw.Root>
  );
}
