// Display on/off indicator in the nav: rendered by the server, then kept fresh here.
// Clicking it opens a panel with the last send attempt (time, content, the Pico's answer, the image).
(function () {
    var el = document.getElementById('displayStatus');
    var panel = document.getElementById('lastSendPanel');
    var labels = { online: 'Display on', offline: 'Display off', unknown: 'Display unknown' };

    function apply(state) {
        if (!labels[state]) state = 'unknown';
        el.className = 'px-display px-display-' + state;
        el.querySelector('.px-display-text').textContent = labels[state];
    }

    function applyLastSend(s) {
        if (!panel) return;
        panel.querySelector('.px-lastsend-empty').hidden = !!s;
        panel.querySelector('.px-lastsend-body').hidden = !s;
        if (!s) return;
        var field = function (name) { return panel.querySelector('[data-field="' + name + '"]'); };
        field('at').textContent = s.at;
        field('content').textContent = s.content;
        field('result').textContent = s.result;
        field('result').className = s.success ? 'px-lastsend-ok' : 'px-lastsend-fail';
        field('duration').textContent = s.durationMs + ' ms';
        var img = panel.querySelector('.px-lastsend-img');
        if (img.getAttribute('src') !== s.image) img.src = s.image;
    }

    window.refreshDisplayStatus = function () {
        if (!el) return;
        fetch(el.dataset.url, { cache: 'no-store' })
            .then(function (r) { return r.json(); })
            .then(function (s) { apply(s.display); applyLastSend(s.lastSend); })
            .catch(function () { /* keep the last known state */ });
    };

    if (!el) return;
    setInterval(window.refreshDisplayStatus, 30000);

    el.addEventListener('click', function (e) {
        e.stopPropagation();
        var open = panel.hidden;
        panel.hidden = !open;
        el.setAttribute('aria-expanded', open ? 'true' : 'false');
        if (open) window.refreshDisplayStatus();
    });
    document.addEventListener('click', function (e) {
        if (!panel.hidden && !panel.contains(e.target)) {
            panel.hidden = true;
            el.setAttribute('aria-expanded', 'false');
        }
    });
})();
