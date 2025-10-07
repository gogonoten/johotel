window.confetti = () => {
    const el = document.createElement('div');
    el.className = 'confetti';
    Object.assign(el.style, { position: 'fixed', inset: 0, pointerEvents: 'none', zIndex: 9999 });
    document.body.appendChild(el);
    const N = 120; const frags = [];
    for (let i = 0; i < N; i++) {
        const s = document.createElement('i');
        s.style.position = 'absolute';
        s.style.left = Math.random() * 100 + '%';
        s.style.top = '-10px';
        s.style.width = '8px';
        s.style.height = '14px';
        s.style.background = `hsl(${Math.random() * 360} 90% 60%)`;
        s.style.transform = `rotate(${Math.random() * 360}deg)`;
        s.style.borderRadius = '2px';
        s.style.opacity = '.9';
        el.appendChild(s); frags.push(s);
    }
    frags.forEach((s) => {
        const t = 800 + Math.random() * 1200;
        s.animate(
            [{ transform: s.style.transform, top: '-10px' }, { transform: `rotate(${Math.random() * 360}deg)`, top: '110%' }],
            { duration: t, easing: 'cubic-bezier(.22,1,.36,1)', fill: 'forwards' }
        );
    });
    setTimeout(() => el.remove(), 2200);
};

window.toast = (msg) => {
    const box = document.createElement('div');
    box.textContent = msg;
    Object.assign(box.style, {
        position: 'fixed', left: '50%', bottom: '28px', transform: 'translateX(-50%)',
        background: '#111827', color: '#fff', padding: '10px 14px', borderRadius: '12px',
        boxShadow: '0 8px 20px rgba(0,0,0,.35)', zIndex: 9999
    });
    document.body.appendChild(box);
    setTimeout(() => box.remove(), 2600);
};
