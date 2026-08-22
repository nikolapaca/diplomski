/**
 * Deterministic "board background" placeholder.
 *
 * Real Trello boards let you set a photo/colour background per board.
 * We don't have that backend feature yet, so every board gets a stable,
 * good-looking gradient derived from its name — same board always
 * renders the same colours, different boards look distinct.
 */
export interface BoardTheme {
  gradient: string;
  solid: string;
}

const BOARD_THEMES: BoardTheme[] = [
  { gradient: 'linear-gradient(135deg, #6a5cf0 0%, #4facfe 100%)', solid: '#6a5cf0' },
  { gradient: 'linear-gradient(135deg, #0fb8ad 0%, #38ef7d 100%)', solid: '#0fb8ad' },
  { gradient: 'linear-gradient(135deg, #f857a6 0%, #ff5858 100%)', solid: '#f857a6' },
  { gradient: 'linear-gradient(135deg, #fa709a 0%, #fbc687 100%)', solid: '#fa709a' },
  { gradient: 'linear-gradient(135deg, #30cfd0 0%, #330867 100%)', solid: '#30cfd0' },
  { gradient: 'linear-gradient(135deg, #ee9ca7 0%, #ffdde1 100%)', solid: '#ee9ca7' },
  { gradient: 'linear-gradient(135deg, #7f7fd5 0%, #86a8e7 50%, #91eae4 100%)', solid: '#7f7fd5' },
  { gradient: 'linear-gradient(135deg, #ff9966 0%, #ff5e62 100%)', solid: '#ff9966' },
  { gradient: 'linear-gradient(135deg, #1fa2ff 0%, #12d8fa 50%, #a6ffcb 100%)', solid: '#1fa2ff' },
  { gradient: 'linear-gradient(135deg, #ff512f 0%, #dd2476 100%)', solid: '#ff512f' },
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
