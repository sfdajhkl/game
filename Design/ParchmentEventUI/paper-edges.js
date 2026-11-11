// Design preview of the same contour and colors used by ParchmentWear.cs.
(() => {
  const book = document.querySelector('.book');
  const canvas = document.createElement('canvas');
  canvas.className = 'paper-edges'; canvas.setAttribute('aria-hidden', 'true'); book.append(canvas);
  function depth(t, length, side) {
    const phase = side * 2.31;
    const edge = 2.8 + 1.1 * Math.sin(t * .019 + phase) + .65 * Math.sin(t * .083 + phase * 1.7) + .3 * Math.sin(t * .37 + phase);
    const corner = Math.max(0, 1 - Math.min(t, length - t) / 17);
    const dent = (t - length * (.19 + side * .137)) / (7 + side * 2);
    const worn = (t - length * (.78 - side * .103)) / 19;
    return edge + 5 * corner * corner + 3.4 * Math.exp(-dent * dent) + 1.8 * Math.exp(-worn * worn);
  }
  function draw() {
    const w = book.clientWidth, h = book.clientHeight, dpr = devicePixelRatio;
    canvas.width = Math.round(w * dpr); canvas.height = Math.round(h * dpr);
    const c = canvas.getContext('2d'); c.scale(dpr, dpr);
    const point = (side, t, d) => side === 0 ? [t,d] : side === 1 ? [w-d,t] : side === 2 ? [w-t,h-d] : [d,h-t];
    for(let side=0;side<4;side++) {
      const length=side%2===0?w:h, steps=Math.min(512,Math.max(1,Math.ceil(length/3)));
      const contour=Array.from({length:steps+1},(_,i)=>{const t=length*i/steps;return [t,depth(t,length,side)];});
      function strip(outerOffset,innerOffset,start,end,cutout=false) {
        const gradient=c.createLinearGradient(...point(side,length/2,2),...point(side,length/2,innerOffset+3));
        gradient.addColorStop(0,start);gradient.addColorStop(1,end);c.fillStyle=cutout?start:gradient;
        c.beginPath();
        contour.forEach(([t,d],i)=>{const p=point(side,t,cutout?0:d+outerOffset);i?c.lineTo(...p):c.moveTo(...p);});
        [...contour].reverse().forEach(([t,d])=>c.lineTo(...point(side,t,d+innerOffset)));
        c.closePath();c.fill();
      }
      strip(0,30,'rgba(121,87,58,.22)','rgba(121,87,58,0)');
      strip(0,.65,'rgba(225,205,171,.38)','rgba(225,205,171,0)');
      strip(0,0,'#3b2a20','#3b2a20',true);
    }
  }
  new ResizeObserver(draw).observe(book);document.fonts.ready.then(draw);
})();
