import fs from 'node:fs';
import path from 'node:path';

const here = path.dirname(new URL(import.meta.url).pathname.replace(/^\/(?=[A-Za-z]:)/, ''));
const root = path.resolve(here, '..', '..');
const data = (p) => `data:image/png;base64,${fs.readFileSync(path.join(root, p)).toString('base64')}`;

const bg = data('Reports/UI/level1-camera.png');
const joystick = data('Assets/Tower-Defense/Art/UI/Images/Controls/joystick-base-painted.png');
const thumb = data('Assets/Tower-Defense/Art/UI/Images/Controls/joystick-thumb-painted.png');
const ultimate = data('Assets/Tower-Defense/Art/UI/Images/Controls/ultimate-face-painted.png');

const svg = `<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="1920" height="900" viewBox="0 0 1920 900">
<title>Tower Defense — подготовка к волне — телефон</title>
<desc>Редактируемый макет HUD. Элементы разложены по группам: верхняя панель, данные врагов, кнопки и управление.</desc>
<defs>
  <linearGradient id="glass" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#102d43" stop-opacity=".96"/><stop offset=".52" stop-color="#092238" stop-opacity=".94"/><stop offset="1" stop-color="#071627" stop-opacity=".98"/></linearGradient>
  <linearGradient id="glassViolet" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#2c2855"/><stop offset="1" stop-color="#121b38"/></linearGradient>
  <linearGradient id="gold" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#fff6a4"/><stop offset=".22" stop-color="#ffe46d"/><stop offset=".65" stop-color="#ffc743"/><stop offset="1" stop-color="#e78e20"/></linearGradient>
  <linearGradient id="cyan" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#baffff"/><stop offset=".38" stop-color="#5ce8f2"/><stop offset="1" stop-color="#149abc"/></linearGradient>
  <linearGradient id="enemyRed" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#fc8c81"/><stop offset="1" stop-color="#ad283f"/></linearGradient>
  <filter id="shadow" x="-50%" y="-50%" width="200%" height="200%"><feDropShadow dx="0" dy="10" stdDeviation="12" flood-color="#03111a" flood-opacity=".72"/></filter>
  <filter id="glow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="8"/></filter>
  <clipPath id="screen"><rect width="1920" height="900"/></clipPath>
</defs>

<g id="01 Gameplay backdrop" clip-path="url(#screen)">
  <image x="0" y="-90" width="1920" height="1080" preserveAspectRatio="xMidYMid slice" xlink:href="${bg}"/>
</g>

<g id="02 Upper HUD" font-family="Segoe UI,Arial,sans-serif" filter="url(#shadow)">
  <g id="Pause button">
    <path d="M35 28 L106 28 L123 45 L123 107 L106 124 L35 124 L18 107 L18 45 Z" fill="#071a2b" stroke="#86eff1" stroke-width="3"/>
    <path d="M41 46 L102 46 L107 51 L107 100 L102 105 L41 105 L36 100 L36 51 Z" fill="#183a4f" stroke="#2ab5c9" stroke-width="2"/>
    <path d="M57 60 h11 v32 H57z M77 60 h11 v32 H77z" fill="#d7fbfa"/>
  </g>
  <g id="Wave badge">
    <path d="M139 29 H430 L453 52 V101 L430 124 H139 L127 111 V42 Z" fill="url(#glass)" stroke="#6ddfe4" stroke-width="3"/>
    <path d="M153 46 H423" stroke="#4ecbd2" stroke-width="2" opacity=".5"/>
    <path d="M151 114 H242 L255 107 H430" stroke="#42c5d2" stroke-width="3" opacity=".5"/>
    <text x="154" y="69" font-size="19" font-weight="800" letter-spacing="2" fill="#8cf5f3">ПОДГОТОВКА</text>
    <text x="154" y="105" font-size="32" font-weight="900" fill="#fff9e9">ВОЛНА 02<tspan font-size="21" fill="#7ddbe5"> / 05</tspan></text>
  </g>
  <g id="Phase status">
    <path d="M796 28 H1114 L1130 45 V91 L1114 108 H796 L780 91 V45 Z" fill="url(#glass)" stroke="#397d93" stroke-width="2.5"/>
    <path d="M814 44 H1095" stroke="#6ef4f1" stroke-width="2.5"/>
    <circle cx="823" cy="70" r="13" fill="#0e728d" stroke="#8cf8f2" stroke-width="2"/>
    <path d="M823 58 V70 L831 76" fill="none" stroke="#f5ffff" stroke-width="3" stroke-linecap="round"/>
    <text x="850" y="78" font-size="21" font-weight="800" letter-spacing="1.3" fill="#e0fcfa">СТРОЙТЕ ОБОРОНУ</text>
  </g>
  <g id="Resources">
    <path d="M1624 29 H1862 L1901 66 V101 L1882 121 H1624 L1605 102 V48 Z" fill="url(#glass)" stroke="#a0dce6" stroke-width="3"/>
    <path d="M1619 61 L1651 35 L1683 61 L1651 88 Z" fill="url(#gold)" stroke="#482b18" stroke-width="4"/>
    <path d="M1651 45 L1667 61 L1651 77 L1635 61 Z" fill="#f7ffff" opacity=".78"/>
    <text x="1702" y="59" font-size="17" font-weight="800" letter-spacing="2" fill="#99d6e6">РЕСУРСЫ</text>
    <text x="1700" y="98" font-size="37" font-weight="900" fill="#fff9e9">12</text>
    <path d="M1810 66 H1880 L1890 78 L1880 101 H1810 Z" fill="#104e61" stroke="#47c8da" stroke-width="1.5"/>
    <text x="1821" y="92" font-size="20" font-weight="800" fill="#83f4e7">+2</text>
  </g>
</g>

<g id="03 Spawn preview in visible range" font-family="Segoe UI,Arial,sans-serif">
  <g id="Spawn world marker" filter="url(#shadow)">
    <path d="M935 171 L956 137 L977 171 Z" fill="url(#enemyRed)" stroke="#571c2b" stroke-width="4"/>
    <circle cx="956" cy="192" r="27" fill="#8f1f32" stroke="#ffb1a3" stroke-width="4"/>
    <circle cx="956" cy="192" r="21" fill="url(#enemyRed)" stroke="#521826" stroke-width="2"/>
    <text x="956" y="201" text-anchor="middle" font-size="27" font-weight="900" fill="#fff8eb">1</text>
    <path d="M979 194 L1044 194" stroke="#fff1ce" stroke-width="3" stroke-dasharray="7 5"/>
  </g>
  <g id="Spawn detail card" filter="url(#shadow)">
    <path d="M1040 139 H1291 L1307 155 V261 L1290 278 H1040 L1026 264 V154 Z" fill="url(#glass)" stroke="#f37c76" stroke-width="3"/>
    <path d="M1041 140 H1290 L1306 155 H1028 Z" fill="#8f283b"/>
    <text x="1049" y="170" font-size="20" font-weight="900" letter-spacing="1.3" fill="#fff4e6">ВЫХОД 1</text>
    <text x="1277" y="169" text-anchor="end" font-size="17" font-weight="800" fill="#ffd9c9">21 м</text>
    <path d="M1048 183 H1284" stroke="#81596b" stroke-width="2"/>
    <g id="Spider count">
      <circle cx="1079" cy="225" r="25" fill="#293647" stroke="#ed7975" stroke-width="3"/>
      <ellipse cx="1079" cy="228" rx="10" ry="9" fill="#d5ecdc"/>
      <circle cx="1079" cy="215" r="6" fill="#d5ecdc"/>
      <path d="M1068 225 l-11 -10 m11 17 l-13 7 m24 -14 l11 -10 m-11 17 l13 7" stroke="#d5ecdc" stroke-width="3" stroke-linecap="round"/>
      <text x="1122" y="235" font-size="29" font-weight="900" fill="#fff8ee">× 12</text>
    </g>
    <g id="Bat count">
      <circle cx="1208" cy="225" r="25" fill="#293647" stroke="#ed7975" stroke-width="3"/>
      <path d="M1190 222 Q1195 213 1202 219 L1208 226 L1214 219 Q1221 213 1226 222 Q1217 225 1214 235 L1208 229 L1202 235 Q1198 225 1190 222 Z" fill="#d5ecdc"/>
      <text x="1240" y="235" font-size="29" font-weight="900" fill="#fff8ee">× 6</text>
    </g>
  </g>
</g>

<g id="04 Main bottom actions" font-family="Segoe UI,Arial,sans-serif" filter="url(#shadow)">
  <g id="Start wave">
    <path d="M782 778 L819 741 H1101 L1138 778 V839 L1101 876 H819 L782 839 Z" fill="#06111b" stroke="#dafafa" stroke-width="3"/>
    <path d="M793 782 L825 752 H1095 L1127 782 V835 L1095 865 H825 L793 835 Z" fill="url(#gold)" stroke="#8e541d" stroke-width="3"/>
    <path d="M823 759 H1098" stroke="#fffad0" stroke-width="4" opacity=".8"/>
    <path d="M829 865 H1087" stroke="#bd6d1f" stroke-width="4" opacity=".7"/>
    <path d="M828 791 L851 811 L828 832 Z" fill="#083047" stroke="#fffae5" stroke-width="3"/>
    <text x="995" y="819" text-anchor="middle" font-size="29" font-weight="950" letter-spacing=".6" fill="#092333">НАЧАТЬ ВОЛНУ</text>
  </g>
  <g id="Teleport home">
    <circle cx="1584" cy="777" r="57" fill="#071b2a" stroke="#8ceaf0" stroke-width="4"/>
    <circle cx="1584" cy="777" r="48" fill="url(#glass)" stroke="#37afc7" stroke-width="2"/>
    <path d="M1559 775 L1584 753 L1609 775 M1566 770 V800 H1602 V770 M1579 800 V784 H1589 V800" fill="none" stroke="#e8ffff" stroke-width="5" stroke-linecap="round" stroke-linejoin="round"/>
    <text x="1584" y="857" text-anchor="middle" font-size="17" font-weight="900" letter-spacing="1.1" fill="#eefefd" stroke="#0b2234" stroke-width="3" paint-order="stroke">ДОМОЙ</text>
  </g>
</g>

<g id="05 Mobile touch controls">
  <g id="Joystick" filter="url(#shadow)">
    <image x="60" y="633" width="228" height="228" xlink:href="${joystick}"/>
    <image x="130" y="703" width="88" height="88" xlink:href="${thumb}"/>
  </g>
  <g id="Ultimate" filter="url(#shadow)">
    <circle cx="1795" cy="752" r="92" fill="#070f25" stroke="#94f9f2" stroke-width="4"/>
    <image x="1712" y="669" width="166" height="166" xlink:href="${ultimate}"/>
    <path d="M1728 807 A84 84 0 0 0 1862 812" fill="none" stroke="#a568e0" stroke-width="9" stroke-linecap="round"/>
    <path d="M1728 807 A84 84 0 0 0 1848 829" fill="none" stroke="#edc5ff" stroke-width="5" stroke-linecap="round"/>
    <path d="M1765 831 H1824 L1834 842 L1824 858 H1765 L1755 842 Z" fill="#16122e" stroke="#c995ec" stroke-width="2"/>
    <text x="1794" y="850" text-anchor="middle" font-family="Segoe UI,Arial,sans-serif" font-size="20" font-weight="900" fill="#fff8ff">85%</text>
  </g>
</g>
</svg>`;

const backdrop = /<g id="01 Gameplay backdrop"[\s\S]*?<\/g>\s*/;
const overlay = svg.replace(backdrop, '');
const output = path.join(here, 'Figma-Preparation-Mobile-UI.svg');
const preview = path.join(here, 'Figma-Preparation-Mobile-Preview.svg');
fs.writeFileSync(output, overlay, 'utf8');
fs.writeFileSync(preview, svg, 'utf8');
console.log(output);
console.log(preview);
