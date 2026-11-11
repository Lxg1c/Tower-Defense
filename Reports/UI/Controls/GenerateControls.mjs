import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
mkdirSync(here, { recursive: true });

const art = {
  'joystick-base': `<defs><radialGradient id="disc"><stop stop-color="#0a2934" stop-opacity=".18"/><stop offset=".66" stop-color="#0b3541" stop-opacity=".54"/><stop offset="1" stop-color="#08202d" stop-opacity=".06"/></radialGradient><linearGradient id="rim" x2="1" y2="1"><stop stop-color="#d3fff4"/><stop offset=".45" stop-color="#68d9d4"/><stop offset="1" stop-color="#267f91"/></linearGradient></defs>
    <circle cx="128" cy="128" r="124" fill="url(#disc)"/><circle cx="128" cy="128" r="119" fill="none" stroke="url(#rim)" stroke-width="3" opacity=".78"/><circle cx="128" cy="128" r="108" fill="none" stroke="#b1fff1" stroke-width="1.8" stroke-opacity=".28"/><circle cx="128" cy="128" r="73" fill="none" stroke="#a0eee8" stroke-width="2" stroke-opacity=".24"/>
    <g stroke="#c6fff0" stroke-width="5" stroke-linecap="round"><path d="M128 7v20M128 229v20M7 128h20M229 128h20"/><path d="M47 47l12 12M197 197l12 12M47 209l12-12M197 59l12-12" opacity=".65"/></g>
    `,
  'joystick-thumb': `<defs><radialGradient id="thumb" cx=".35" cy=".28" r=".8"><stop stop-color="#c7fff6"/><stop offset=".18" stop-color="#74e7df"/><stop offset=".58" stop-color="#2a9aab"/><stop offset="1" stop-color="#0b3b52"/></radialGradient></defs>
    <circle cx="128" cy="132" r="111" fill="#071d2b" opacity=".56"/><circle cx="128" cy="123" r="111" fill="url(#thumb)" stroke="#e4fff8" stroke-width="7"/><circle cx="128" cy="123" r="96" fill="none" stroke="#e2fff7" stroke-opacity=".38" stroke-width="5"/><circle cx="128" cy="123" r="48" fill="#d7fff2" fill-opacity=".54" stroke="#e7fff9" stroke-width="4"/><circle cx="128" cy="123" r="12" fill="#ecfff9"/>`,
  'ultimate-face': `<defs><radialGradient id="face"><stop stop-color="#783f9f"/><stop offset=".56" stop-color="#3a245d"/><stop offset="1" stop-color="#11172b"/></radialGradient><linearGradient id="metal" x2="1" y2="1"><stop stop-color="#fff2ff"/><stop offset=".45" stop-color="#b887db"/><stop offset="1" stop-color="#6a478f"/></linearGradient></defs>
    <circle cx="128" cy="128" r="122" fill="#0b1020" stroke="#50346d" stroke-width="5"/><circle cx="128" cy="128" r="114" fill="url(#face)" stroke="url(#metal)" stroke-width="7"/><circle cx="128" cy="128" r="98" fill="none" stroke="#e6c4fc" stroke-opacity=".42" stroke-width="2"/><path d="M128 13 139 32 128 39 117 32ZM128 243l11-19-11-7-11 7ZM13 128l19-11 7 11-7 11ZM243 128l-19-11-7 11 7 11Z" fill="#efd1ff"/><path d="M128 46 177 75 194 128 177 181 128 210 79 181 62 128 79 75Z" fill="#2e1e4d" stroke="#bf8fe2" stroke-width="5"/><path d="M128 61 165 83 181 128 165 173 128 195 91 173 75 128 91 83Z" fill="url(#face)" stroke="#e1b9fa" stroke-opacity=".45" stroke-width="2"/><path d="m128 68 29 39 40 10-31 31 5 43-43-20-43 20 5-43-31-31 40-10z" fill="#a76ed1" stroke="#f4d9ff" stroke-width="4"/><path d="m133 87-28 51h21l-6 39 35-59h-25z" fill="#f8e4ff" stroke="#ffffff" stroke-width="3"/>`,
};

for (const [name, body] of Object.entries(art)) {
  writeFileSync(join(here, `${name}.svg`), `<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256" viewBox="0 0 256 256">${body}</svg>`);
}
