function celebrateDay() {
    const overlay = document.getElementById('celebration-overlay');
    if (!overlay) return;

    const colors = ['#4f46e5', '#059669', '#d97706', '#dc2626', '#0891b2', '#7c3aed', '#eab308'];
    const pieces = [];

    // Confetti: small falling rectangles
    for (let i = 0; i < 60; i++) {
        const el = document.createElement('div');
        el.className = 'confetti-piece';
        el.style.left = Math.random() * 100 + 'vw';
        el.style.background = colors[Math.floor(Math.random() * colors.length)];
        el.style.animationDuration = (2 + Math.random() * 1.5) + 's';
        el.style.animationDelay = (Math.random() * 0.6) + 's';
        el.style.transform = `rotate(${Math.random() * 360}deg)`;
        overlay.appendChild(el);
        pieces.push(el);
    }

    // Balloons: rising circles with a string
    const balloonEmojis = ['🎈', '🎉', '👏'];
    for (let i = 0; i < 14; i++) {
        const el = document.createElement('div');
        el.className = 'balloon-piece';
        el.textContent = balloonEmojis[Math.floor(Math.random() * balloonEmojis.length)];
        el.style.left = (5 + Math.random() * 90) + 'vw';
        el.style.animationDuration = (3 + Math.random() * 1.5) + 's';
        el.style.animationDelay = (Math.random() * 0.8) + 's';
        el.style.fontSize = (24 + Math.random() * 20) + 'px';
        overlay.appendChild(el);
        pieces.push(el);
    }

    setTimeout(() => {
        pieces.forEach(p => p.remove());
    }, 4500);
}
