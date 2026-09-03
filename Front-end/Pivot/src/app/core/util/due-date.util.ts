export type DueDateStatus = 'overdue' | 'soon' | 'upcoming';

const SOON_THRESHOLD_MS = 24 * 60 * 60 * 1000;
export function getDueDateStatus(dueDate: string | null | undefined): DueDateStatus | null {
  if (!dueDate) {
    return null;
  }

  const dueTime = new Date(dueDate).getTime();
  if (isNaN(dueTime)) {
    return null;
  }

  const diffMs = dueTime - Date.now();

  if (diffMs < 0) {
    return 'overdue';
  }
  if (diffMs <= SOON_THRESHOLD_MS) {
    return 'soon';
  }
  return 'upcoming';
}

export function toDatetimeLocalValue(dueDate: string | null | undefined): string {
  if (!dueDate) {
    return '';
  }

  const date = new Date(dueDate);
  if (isNaN(date.getTime())) {
    return '';
  }

  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function nowAsDatetimeLocalValue(): string {
  return toDatetimeLocalValue(new Date().toISOString());
}

export function fromDatetimeLocalValue(value: string): string | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (isNaN(date.getTime())) {
    return null;
  }

  return date.toISOString();
}