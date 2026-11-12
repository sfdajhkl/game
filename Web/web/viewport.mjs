// Fit the reading area without cropping content or shrinking the whole game.
// Overflow is paginated at semantic boundaries; navigation never changes game state.
const pageMemory = new Map();
const structural = '.event-grid,.event-grid>section,.item-list,.item-list>article,.companion-list,.companion-list>article,.journal,.journal>article,.ending,.choices,.battle-bottom,.battle-bottom>section,.skills,.mobile-actions';

export function fitPages(view, controls, key) {
  const template = [...view.children].map(node => node.cloneNode(true));
  let disposed = false, scheduled = 0, pages = [], page = pageMemory.get(key) || 0;
  let lastSize = '';
  const fits = () => {
    // Each illustration pixel occupies an integer number of physical pixels.
    const dpr = window.devicePixelRatio || 1;
    for (const picture of view.querySelectorAll('.picture')) {
      const img = picture.querySelector('img');
      const grid = picture.closest('.event-grid');
      const style = grid && window.getComputedStyle?.(grid);
      const stacked = !style || style.display !== 'grid' || style.gridTemplateColumns.trim().split(/\s+/).length < 2;
      const choices = grid?.querySelector('.event-choices');
      const reserved = choices?.getBoundingClientRect().height || 0;
      // Side-by-side art can use the entire story row, with room for actions.
      // Stacked/paginated views reserve reading space instead of making art a full page.
      const cap = stacked ? view.clientHeight * .45 : Math.max(128, view.clientHeight - reserved - 24);
      const n = Math.max(1, Math.floor(Math.min(picture.clientWidth / 192, cap / 128) * dpr));
      img.style.width = `${192 * n / dpr}px`; img.style.height = `${128 * n / dpr}px`;
    }
    return view.scrollHeight <= view.clientHeight + 1 && view.scrollWidth <= view.clientWidth + 1;
  };
  const clone = nodes => nodes.map(node => node.cloneNode(true));
  function show(index) {
    page = Math.max(0, Math.min(index, pages.length - 1));
    pageMemory.set(key, page);
    if (pageMemory.size > 64) pageMemory.delete(pageMemory.keys().next().value);
    view.replaceChildren(...clone(pages[page] || []));
    fits();
    controls.replaceChildren();
    controls.hidden = pages.length < 2;
    if (controls.hidden) return;
    const previous = document.createElement('button'), next = document.createElement('button'), count = document.createElement('span');
    previous.textContent = '← 이전'; next.textContent = '다음 →';
    previous.disabled = page === 0; next.disabled = page === pages.length - 1;
    previous.setAttribute('aria-label', '이전 화면'); next.setAttribute('aria-label', '다음 화면');
    count.textContent = `${page + 1} / ${pages.length}`;
    count.setAttribute('aria-live', 'polite');
    const navigate = (index, direction) => {
      show(index);
      const buttons = controls.querySelectorAll('button');
      const preferred = buttons[direction];
      (preferred?.disabled ? buttons[1 - direction] : preferred)?.focus({preventScroll:true});
    };
    previous.onclick = () => navigate(page - 1, 0);
    next.onclick = () => navigate(page + 1, 1);
    controls.append(previous, count, next);
  }
  // Keep the original wrappers so narrative/UI font roles survive pagination.
  function flatten(node) {
    if (!node.matches(structural) || !node.children.length) return [node.cloneNode(true)];
    return [...node.children].flatMap(child => flatten(child).map(piece => {
      const wrapper = node.cloneNode(false);
      wrapper.classList.add('page-fragment');
      wrapper.append(piece);
      return wrapper;
    }));
  }
  function measure(node) { view.replaceChildren(node); return fits(); }
  function splitOversized(node) {
    if (measure(node.cloneNode(true))) return [node];
    // Unwrap an oversized group while preserving its styling context.
    let leaf = node;
    while (leaf.children.length === 1 && !leaf.matches('button,p,h1,h2,small,label')) leaf = leaf.firstElementChild;
    if (leaf.children.length > 1 && !leaf.matches('button,p,h1,h2')) {
      return [...leaf.children].flatMap(child => {
        const piece = node.cloneNode(true);
        let target = piece;
        while (target.children.length === 1 && !target.matches('button,p,h1,h2,small,label')) target = target.firstElementChild;
        target.replaceChildren(child.cloneNode(true));
        return splitOversized(piece);
      });
    }
    // Very long prose can span pages without dropping a character. Interactive
    // controls remain intact; all shipped choices fit at the supported sizes.
    if (leaf.matches('p,h1,h2,small') && !leaf.children.length && leaf.textContent.length > 1) {
      const text = leaf.textContent;
      const withText = value => {
        const piece = node.cloneNode(true); let target = piece;
        while (target.children.length === 1 && !target.matches('button,p,h1,h2,small,label')) target = target.firstElementChild;
        target.textContent = value; return piece;
      };
      let lo = 1, hi = text.length - 1, best = 0;
      while (lo <= hi) {
        const mid = Math.floor((lo + hi) / 2);
        if (measure(withText(text.slice(0, mid)))) { best = mid; lo = mid + 1; } else hi = mid - 1;
      }
      if (best > 0) {
        const space = text.lastIndexOf(' ', best - 1);
        if (space > best * .65) best = space + 1;
        return [withText(text.slice(0, best)), ...splitOversized(withText(text.slice(best)))];
      }
    }
    return [node];
  }
  function layout() {
    scheduled = 0;
    if (disposed || !view.isConnected) return;
    controls.hidden = true;
    view.classList.remove('paged');
    view.replaceChildren(...clone(template));
    if (fits()) { pages = [clone(template)]; page = 0; show(0); lastSize = `${view.clientWidth}:${view.clientHeight}`; return; }
    controls.hidden = false;
    view.classList.add('paged');
    const blocks = template.flatMap(flatten).flatMap(splitOversized);
    pages = []; let current = [];
    view.replaceChildren();
    for (const block of blocks) {
      view.append(block.cloneNode(true));
      if (!fits() && current.length) {
        pages.push(current); current = [];
        view.replaceChildren(block.cloneNode(true));
      }
      current.push(block);
    }
    if (current.length) pages.push(current);
    show(page);
    lastSize = `${view.clientWidth}:${view.clientHeight}`;
  }
  function schedule() { if (!disposed && !scheduled) scheduled = requestAnimationFrame(layout); }
  const observer = new ResizeObserver(() => {
    if (`${view.clientWidth}:${view.clientHeight}` !== lastSize) schedule();
  });
  observer.observe(view);
  document.fonts.ready.then(schedule);
  window.addEventListener('resize', schedule);
  layout();
  return () => { disposed = true; cancelAnimationFrame(scheduled); observer.disconnect(); window.removeEventListener('resize', schedule); };
}
