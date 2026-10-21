(() => {
  const labels = {
    idle: 'No microphone or camera use detected',
    mic: 'Microphone activity detected',
    camera: 'Camera activity detected',
    both: 'Microphone + camera detected'
  };
  const dot = document.getElementById('signal');
  const status = document.getElementById('signal-state');
  document.querySelectorAll('[data-state]').forEach(button => {
    button.addEventListener('click', () => {
      const state = button.dataset.state;
      if (!Object.hasOwn(labels, state) || !dot || !status) return;
      dot.className = `signal-dot ${state}`;
      status.textContent = labels[state];
      document.querySelectorAll('[data-state]').forEach(item => {
        item.setAttribute('aria-pressed', String(item === button));
      });
    });
  });
  document.querySelectorAll('[data-theme]').forEach(button => {
    button.addEventListener('click', () => {
      const theme = button.dataset.theme;
      if (theme !== 'dark' && theme !== 'light') return;
      const screenshot = document.getElementById('app-screenshot');
      const mat = document.getElementById('screenshot-mat');
      if (!screenshot || !mat) return;
      screenshot.src = `screenshots/popup-${theme}.png`;
      screenshot.alt = `PrivacyDot ${theme} theme popup showing illustrative detected microphone and camera apps grouped by device`;
      mat.classList.toggle('light', theme === 'light');
      document.querySelectorAll('[data-theme]').forEach(item => {
        item.setAttribute('aria-pressed', String(item === button));
      });
    });
  });
})();
