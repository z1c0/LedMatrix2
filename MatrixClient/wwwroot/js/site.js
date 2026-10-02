// Display on/off indicator in the nav: rendered by the server, then kept fresh here
(function () {
    var el = document.getElementById('displayStatus');
    var labels = { online: 'Display on', offline: 'Display off', unknown: 'Display unknown' };

    function apply(state) {
        if (!labels[state]) state = 'unknown';
        el.className = 'px-display px-display-' + state;
        el.querySelector('.px-display-text').textContent = labels[state];
    }

    window.refreshDisplayStatus = function () {
        if (!el) return;
        fetch(el.dataset.url, { cache: 'no-store' })
            .then(function (r) { return r.json(); })
            .then(function (s) { apply(s.display); })
            .catch(function () { /* keep the last known state */ });
    };

    if (el) setInterval(window.refreshDisplayStatus, 30000);
})();
