export interface BoardTheme {
  gradient: string;
  solid: string;
}

const BOARD_THEMES: BoardTheme[] = [
  { gradient: '#1f3a5f', solid: '#1f3a5f' }, // ink
  { gradient: '#a63a2e', solid: '#a63a2e' }, // stamp
  { gradient: '#4c6b43', solid: '#4c6b43' }, // moss
  { gradient: '#a67c1f', solid: '#a67c1f' }, // ochre
  { gradient: '#5c4a72', solid: '#5c4a72' }, // plum
  { gradient: '#2d6a6a', solid: '#2d6a6a' }, // teal
  { gradient: '#7a3b1e', solid: '#7a3b1e' }, // rust
  { gradient: '#3f4a5c', solid: '#3f4a5c' }, // slate
];

function hashString(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

export function getBoardTheme(seed: string | null | undefined): BoardTheme {
  const key = seed && seed.length ? seed : 'default';
  const index = hashString(key) % BOARD_THEMES.length;
  return BOARD_THEMES[index];
}
