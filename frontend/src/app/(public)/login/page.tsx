// @Role: Strona /login | @DependsOn: features/accounts | @UsedBy: Next.js App Router (trasa /login) | @Task: TASK-133
import { LoginForm } from "@/features/accounts";

export default function StronaLogowania() {
  return (
    <main className="mx-auto max-w-md p-4">
      <LoginForm />
    </main>
  );
}
