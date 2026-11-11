const screens = [...document.querySelectorAll('.screen')];
const tabs = [...document.querySelectorAll('.review-tab')];
const spawnStateToggle = document.querySelector('.spawn-state-toggle');
const buildStateToggle = document.querySelector('.build-state-toggle');
let previousScreen = 'desktop';

function positionSpawnPointer(screen) {
  if (screen?.dataset.spawnState !== 'far') return;
  const marker = screen.querySelector('.spawn-far .spawn-number');
  const pointer = marker.querySelector('.spawn-pointer');
  const screenRect = screen.getBoundingClientRect();
  const markerRect = marker.getBoundingClientRect();
  let dx = screenRect.left + screenRect.width * Number(screen.dataset.spawnX) - (markerRect.left + markerRect.width / 2);
  let dy = screenRect.top + screenRect.height * Number(screen.dataset.spawnY) - (markerRect.top + markerRect.height / 2);
  const distance = Math.hypot(dx, dy);
  if (distance < 0.001) { dx = 0; dy = -1; }
  const length = Math.max(distance, 1);
  // The wide base touches the circle; the triangle itself remains outside the number.
  const radius = Math.min(marker.offsetWidth, marker.offsetHeight) / 2 + pointer.offsetHeight / 2;
  const offsetX = dx / length * radius;
  const offsetY = dy / length * radius;
  pointer.style.left = '50%';
  pointer.style.top = '50%';
  pointer.style.transform = `translate(calc(-50% + ${offsetX}px), calc(-50% + ${offsetY}px)) rotate(${Math.atan2(dy, dx) * 180 / Math.PI + 90}deg)`;
}

function updateSpawnStateToggle() {
  const active = document.querySelector('.screen.is-active');
  const isPreparation = active?.id === 'desktop' || active?.id === 'mobile';
  spawnStateToggle.hidden = !isPreparation;
  if (isPreparation) spawnStateToggle.textContent = active.dataset.spawnState === 'near' ? 'Вход: виден' : 'Вход: за кадром';
  positionSpawnPointer(active);
}

function updateBuildStateToggle() {
  const active = document.querySelector('.screen.is-active');
  buildStateToggle.hidden = active?.id !== 'building';
  if (active?.id !== 'building') return;
  const state = active.dataset.buildState;
  buildStateToggle.textContent = state === 'choice' ? 'Точка: выбор' : state === 'single' ? 'Точка: один вариант' : 'Точка: открытие';
  const anchor = active.querySelector('.build-anchor');
  anchor.querySelector('img').src = state === 'single'
    ? '../../Assets/Tower-Defense/Art/UI/Images/ZoneIcons/mine.png'
    : '../../Assets/Tower-Defense/Art/UI/Images/ZoneIcons/defense.png';
}

function showScreen(id) {
  const current = document.querySelector('.screen.is-active');
  if (id === 'loadout' && current?.id !== 'loadout') previousScreen = current?.id || 'desktop';
  screens.forEach(screen => screen.classList.toggle('is-active', screen.id === id));
  tabs.forEach(tab => tab.classList.toggle('active', tab.dataset.view === id));
  updateSpawnStateToggle();
  updateBuildStateToggle();
}

tabs.forEach(tab => tab.addEventListener('click', () => showScreen(tab.dataset.view)));
document.querySelector('.back-button').addEventListener('click', () => showScreen(previousScreen));
spawnStateToggle.addEventListener('click', () => {
  const active = document.querySelector('.screen.is-active');
  active.dataset.spawnState = active.dataset.spawnState === 'near' ? 'far' : 'near';
  updateSpawnStateToggle();
});
buildStateToggle.addEventListener('click', () => {
  const building = document.getElementById('building');
  const states = ['progress', 'choice', 'single'];
  building.dataset.buildState = states[(states.indexOf(building.dataset.buildState) + 1) % states.length];
  updateBuildStateToggle();
});
const parameters = new URLSearchParams(location.search);
const initialView = parameters.get('view');
if (screens.some(screen => screen.id === initialView)) showScreen(initialView);
const initialBuildState = parameters.get('build');
if (['progress', 'choice', 'single'].includes(initialBuildState)) {
  document.getElementById('building').dataset.buildState = initialBuildState;
  updateBuildStateToggle();
}
const initialSpawnState = parameters.get('spawn');
if (initialSpawnState === 'near' || initialSpawnState === 'far') {
  document.querySelector('.screen.is-active').dataset.spawnState = initialSpawnState;
}
updateSpawnStateToggle();
updateBuildStateToggle();
window.addEventListener('resize', () => positionSpawnPointer(document.querySelector('.screen.is-active')));
if (parameters.has('export')) document.body.classList.add('export');
document.querySelectorAll('[data-next]').forEach(button =>
  button.addEventListener('click', () => showScreen(button.dataset.next)));
document.querySelectorAll('.close-control').forEach(button =>
  button.addEventListener('click', () => showScreen('mobile')));
document.querySelectorAll('.rally-action').forEach(button => button.addEventListener('click', () => {
  const following = button.getAttribute('aria-pressed') !== 'true';
  document.querySelectorAll('.rally-action').forEach(other => {
    other.setAttribute('aria-pressed', String(following));
    other.classList.toggle('is-following', following);
    other.querySelector('.utility-caption').textContent = following ? 'СЛЕДУЮТ' : 'ЗА МНОЙ';
  });
}));
document.querySelectorAll('.teleport-action').forEach(button => button.addEventListener('click', () => {
  button.classList.add('is-pulsing');
  window.setTimeout(() => button.classList.remove('is-pulsing'), 450);
}));
document.querySelectorAll('.build-card').forEach(card =>
  card.addEventListener('click', () => {
    document.querySelectorAll('.build-card').forEach(other => other.classList.remove('selected'));
    card.classList.add('selected');
  }));
document.querySelector('.single-build-button').addEventListener('click', event => {
  event.currentTarget.innerHTML = 'ПОСТРОЕНО';
  event.currentTarget.disabled = true;
});
const loadoutStage = document.querySelector('.loadout-stage');
function updateHeroImage() {
  const armor = loadoutStage.dataset.appearance === 'assault';
  const leftHeavy = loadoutStage.dataset.leftWeapon === 'heavy';
  const rightHeavy = loadoutStage.dataset.rightWeapon === 'heavy';
  const image = leftHeavy && rightHeavy
    ? (armor ? 'r07-armor-both-heavy-concept.png' : 'r07-both-heavy-concept.png')
    : leftHeavy
      ? (armor ? 'r07-armor-left-heavy-concept.png' : 'r07-left-heavy-concept.png')
      : rightHeavy
        ? (armor ? 'r07-combined-concept.png' : 'r07-heavy-weapon-concept.png')
        : (armor ? 'r07-armor-concept.png' : 'r07-current.png');
  loadoutStage.querySelector('.hero-visual img').src = image;
}
function showInventory(category, side = 'left') {
  document.querySelectorAll('[data-inventory]').forEach(tab => tab.classList.toggle('active', tab.dataset.inventory === category));
  document.querySelectorAll('[data-set]').forEach(set => set.classList.toggle('is-active', set.dataset.set === category));
  document.querySelectorAll('.gear-slot').forEach(slot => slot.classList.toggle('active', slot.dataset.category === category && (category !== 'weapon' || slot.dataset.side === side)));
  document.querySelectorAll('.weapon-column').forEach(column => column.classList.toggle('is-focused', column.dataset.side === side));
}
document.querySelectorAll('[data-inventory]').forEach(tab => tab.addEventListener('click', () => showInventory(tab.dataset.inventory)));
document.querySelectorAll('.gear-slot').forEach(slot => slot.addEventListener('click', () => showInventory(slot.dataset.category, slot.dataset.side)));
document.querySelectorAll('.equipment-card').forEach(card => card.addEventListener('click', () => {
  const set = card.closest('.inventory-set');
  const group = set.dataset.set === 'weapon' ? card.closest('.weapon-column') : set;
  group.querySelectorAll('.equipment-card').forEach(other => { other.classList.remove('selected'); other.querySelector('em').textContent = set.dataset.set === 'module' || set.dataset.set === 'weapon' ? 'ВЫБРАТЬ' : 'ПРИМЕРИТЬ'; });
  card.classList.add('selected');
  card.querySelector('em').textContent = set.dataset.set === 'module' ? 'ВЫБРАН' : 'НАДЕТО';
  if (set.dataset.set === 'armor') {
    const assault = card.dataset.item === 'assault';
    loadoutStage.dataset.appearance = card.dataset.item;
    updateHeroImage();
    document.querySelector('#armor-slot-name').textContent = assault ? 'ШТУРМОВОЙ' : 'БАЗОВЫЙ';
  } else if (set.dataset.set === 'weapon') {
    const heavy = card.dataset.item === 'heavy';
    const side = group.dataset.side;
    loadoutStage.dataset[`${side}Weapon`] = card.dataset.item;
    updateHeroImage();
    document.querySelector(`#${side}-weapon-name`).textContent = heavy ? 'ТЯЖЁЛАЯ' : 'СТАНДАРТ';
    showInventory('weapon', side);
  } else {
    const overdrive = card.dataset.item === 'overdrive';
    document.querySelector('#module-slot-name').textContent = overdrive ? 'ФОРСАЖ' : 'БАЗОВЫЙ';
  }
}));
const initialArmor = new URLSearchParams(location.search).get('armor');
if (initialArmor === 'assault') document.querySelector('[data-set="armor"] [data-item="assault"]').click();
const previewParams = new URLSearchParams(location.search);
const initialLeftWeapon = previewParams.get('left') ?? previewParams.get('weapon');
const initialRightWeapon = previewParams.get('right') ?? previewParams.get('weapon');
if (initialLeftWeapon === 'heavy') document.querySelector('.weapon-column[data-side="left"] [data-item="heavy"]').click();
if (initialRightWeapon === 'heavy') document.querySelector('.weapon-column[data-side="right"] [data-item="heavy"]').click();
if (previewParams.get('tab') === 'weapon') showInventory('weapon');

document.querySelectorAll('.joystick').forEach(joystick => {
  function moveThumb(event) {
    const rect = joystick.getBoundingClientRect();
    const scale = rect.width / 132;
    const x = (event.clientX - rect.left - rect.width / 2) / scale;
    const y = (event.clientY - rect.top - rect.height / 2) / scale;
    const distance = Math.max(1, Math.hypot(x, y));
    const radius = Math.min(33, distance);
    joystick.style.setProperty('--joy-x', `${x / distance * radius}px`);
    joystick.style.setProperty('--joy-y', `${y / distance * radius}px`);
    joystick.style.setProperty('--joy-angle', `${Math.atan2(y, x) * 180 / Math.PI + 90}deg`);
  }
  joystick.addEventListener('pointerdown', event => { joystick.setPointerCapture(event.pointerId); joystick.classList.add('is-dragging'); moveThumb(event); });
  joystick.addEventListener('pointermove', event => { if (joystick.classList.contains('is-dragging')) moveThumb(event); });
  function resetThumb() { joystick.classList.remove('is-dragging'); joystick.style.removeProperty('--joy-x'); joystick.style.removeProperty('--joy-y'); joystick.style.removeProperty('--joy-angle'); }
  joystick.addEventListener('pointerup', resetThumb);
  joystick.addEventListener('pointercancel', resetThumb);
});
document.querySelectorAll('.radial-ultimate').forEach(button => button.addEventListener('click', () => {
  const ready = button.classList.toggle('ready');
  button.style.setProperty('--charge', ready ? '100%' : '85%');
  button.querySelector('.ult-percent').innerHTML = ready ? '100<small>%</small>' : '85<small>%</small>';
  button.setAttribute('aria-label', ready ? 'Ультимейт готов' : 'Ультимейт, заряд 85%');
}));

document.querySelectorAll('.pause').forEach(button => button.addEventListener('click', () => {
  const screen = button.closest('.screen');
  if (screen.querySelector('.prototype-pause')) return;
  const overlay = document.createElement('div');
  overlay.className = 'prototype-pause';
  overlay.innerHTML = '<div><small>ПАУЗА / ПРОТОТИП</small><h2>ИГРА ПРИОСТАНОВЛЕНА</h2><button>ПРОДОЛЖИТЬ</button></div>';
  overlay.querySelector('button').addEventListener('click', () => overlay.remove());
  screen.append(overlay);
}));
