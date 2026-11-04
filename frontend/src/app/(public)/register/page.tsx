// @Role: Strona /register | @DependsOn: features/accounts | @UsedBy: Next.js App Router (trasa /register) | @Task: TASK-133
import { RegisterForm } from "@/features/accounts";

export default function StronaRejestracji() {
  return (
    <main className="mx-auto max-w-md p-4">
      <RegisterForm />
    </main>
  );
}
