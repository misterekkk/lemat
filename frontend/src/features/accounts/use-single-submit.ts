// @Role: Hook blokujący równoległe wysyłki formularza (podwójne kliknięcie): blokada trwa do błędu, bo po sukcesie następuje przekierowanie | @DependsOn: react | @UsedBy: features/accounts/LoginForm, features/accounts/RegisterForm | @Task: TASK-133
"use client";

import { useCallback, useRef } from "react";

export function useJednaWysylka(): <T>(akcja: () => Promise<T>) => Promise<T | undefined> {
  const zajety = useRef(false);
  return useCallback(async <T>(akcja: () => Promise<T>) => {
    if (zajety.current) return undefined;
    zajety.current = true;
    try {
      return await akcja();
    } catch (e) {
      zajety.current = false;
      throw e;
    }
  }, []);
}
