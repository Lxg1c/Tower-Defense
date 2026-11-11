import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
mkdirSync(here, { recursive: true });

const icons = {
  townhall: {
    color: '#72F5EB',
    glow: '#1AA4AC',
    glyph: `<g stroke="currentColor" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" fill="none">
      <path d="M128 50v29M128 50l-11-11m11 11 11-11M65 126l31-19M191 126l-31-19M128 172v22"/>
      <path d="M128 76 170 101v49l-42 26-42-26v-49z"/>
      <path d="M128 91 155 107v33l-27 17-27-17v-33z" stroke-width="6"/>
      <circle cx="128" cy="50" r="8" fill="currentColor" stroke="none"/><circle cx="61" cy="128" r="9" fill="currentColor" stroke="none"/><circle cx="195" cy="128" r="9" fill="currentColor" stroke="none"/>
    </g>`,
  },
  defense: {
    color: '#FFD16C',
    glow: '#A2691C',
    glyph: `<g stroke="currentColor" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" fill="none">
      <path d="M63 185h130M80 184l13-40h70l13 40M95 141v-27l33-23 33 23v27"/>
      <path d="M109 97V63h16v35M133 97V63h16v35"/><path d="M70 93 52 76M186 93l18-17" stroke-width="6"/>
      <circle cx="128" cy="126" r="10" fill="currentColor" stroke="none"/>
    </g>`,
  },
  mine: {
    color: '#B4EE83',
    glow: '#5F9E39',
    glyph: `<g stroke="currentColor" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" fill="none">
      <path d="M60 184h136M78 184l14-30h72l14 30M96 112l32-45 32 45-32 43z"/>
      <path d="M128 68v86M96 112h64M128 155l-26-43M128 155l26-43" stroke-width="5"/>
      <path d="M73 120 53 107M183 120l20-13" stroke-width="6"/>
    </g>`,
  },
  barracks: {
    color: '#D5AAFF',
    glow: '#7B4BAA',
    glyph: `<g stroke="currentColor" stroke-width="9" stroke-linecap="round" stroke-linejoin="round" fill="none">
      <path d="M85 71h86l15 36-13 58-45 24-45-24-13-58z"/>
      <path d="M96 112h64v25H96z"/><path d="M113 113v23M143 113v23" stroke-width="5"/>
      <path d="M102 76V52M154 76V52M74 104 55 87M182 104l19-17" stroke-width="6"/>
      <path d="M109 165h38"/>
    </g>`,
  },
  wall: {
    color: '#9CCAFF',
    glow: '#4C83BD',
    glyph: `<g stroke="currentColor" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" fill="none">
      <path d="M48 176h160M56 94h144v82H56zM56 121h144M56 149h144M102 94v27M154 94v27M81 121v28M128 121v28M175 121v28M102 149v27M154 149v27"/>
      <path d="M49 88h20v-22h23v22h24v-22h24v22h24v-22h23v22h20"/>
    </g>`,
  },
};

for (const [name, icon] of Object.entries(icons)) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256" style="color:${icon.color}">
  <defs>
    <linearGradient id="plate" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#173B4E"/><stop offset=".55" stop-color="#0A2231"/><stop offset="1" stop-color="#061521"/></linearGradient>
    <filter id="glow" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="5"/></filter>
  </defs>
  <path d="M48 13h160l35 35v160l-35 35H48l-35-35V48z" fill="url(#plate)" stroke="${icon.color}" stroke-width="5"/>
  <path d="M57 26h143l30 30v144l-30 30H57l-30-30V56z" fill="none" stroke="${icon.color}" stroke-opacity=".26" stroke-width="3"/>
  <path d="M52 24h38M166 232h38" stroke="${icon.color}" stroke-width="8" stroke-linecap="round"/>
  <circle cx="128" cy="128" r="73" fill="${icon.glow}" opacity=".12"/>
  <g style="color:${icon.color}" opacity=".5" filter="url(#glow)">${icon.glyph}</g>
  <g style="color:${icon.color}">${icon.glyph}</g>
</svg>`;
  writeFileSync(join(here, `${name}.svg`), svg);
}
