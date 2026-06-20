// SignalFlow — Real-time pipeline monitor + interactive UI
(function () {
    'use strict';

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/pipeline')
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    const pipelineBtn = document.getElementById('iq-run-pipeline');
    const statusEl = document.getElementById('iq-pipeline-status');
    const progressEl = document.getElementById('iq-pipeline-progress');
    const logEl = document.getElementById('iq-pipeline-log');
    const modalEl = document.getElementById('iqReclassifyModal');
    const modal = modalEl ? new bootstrap.Modal(modalEl) : null;
    const signalSelect = document.getElementById('iq-reclassify-signal');
    const confirmBtn = document.getElementById('iq-reclassify-confirm');
    const noiseBtn = document.getElementById('iq-reclassify-noise-btn');

    let currentSignalNames = [];
    let reclassifyItemId = null;

    function appendLog(message, level) {
        if (!logEl) return;
        const icons = { info: '\u2139', success: '\u2713', warn: '\u26A0', error: '\u274C' };
        const line = document.createElement('div');
        line.className = 'log-' + (level || 'info');
        line.textContent = (icons[level] || '') + ' ' + message;
        logEl.appendChild(line);
        logEl.scrollTop = logEl.scrollHeight;
    }

    // --- Sidebar update ---
    function updateSidebar(signalCounts) {
        const sidebar = document.querySelector('.iq-signalflow__sidebar ul.nav');
        if (!sidebar) return;
        sidebar.innerHTML = '';
        for (const [signal, count] of Object.entries(signalCounts)) {
            const li = document.createElement('li');
            li.className = 'nav-item';
            li.innerHTML = '<a class="nav-link" href="/signalflow/signals/' + encodeURIComponent(signal) + '">' +
                '<span>&#128193; ' + signal + '</span>' +
                '<span class="badge bg-secondary rounded-pill">' + count + '</span></a>';
            sidebar.appendChild(li);
        }
    }

    function incrementSidebarCount(signalName) {
        const sidebar = document.querySelector('.iq-signalflow__sidebar ul.nav');
        if (!sidebar) return;
        const links = sidebar.querySelectorAll('.nav-link');
        for (const link of links) {
            const span = link.querySelector('span');
            if (span && span.textContent.trim() === signalName) {
                const badge = link.querySelector('.badge');
                if (badge) {
                    badge.textContent = parseInt(badge.textContent, 10) + 1;
                }
                return;
            }
        }
    }

    function updateSidebarLink(selector, text) {
        const el = document.querySelector(selector);
        if (el) el.textContent = text;
    }

    function updateDashboard() {
        connection.invoke('GetDashboardData').catch(function (err) {
            console.error('GetDashboardData failed:', err);
        });
    }

    // --- Recent items rendering ---
    function buildItemCardHtml(id, title, signal, isNoise, reasoning, classifiedAt) {
        var signalBadge = isNoise
            ? '<span class="text-warning">&#9888; Noise</span>'
            : '<span class="iq-signalflow__item-card__signal" style="background:#6c757d;color:#fff">&#128193; ' + escapeHtml(signal) + '</span>';
        var timeStr = classifiedAt ? new Date(classifiedAt).toLocaleString() : '';
        return '<div class="iq-signalflow__item-card mb-2">' +
            '<div class="iq-signalflow__item-card__body" onclick="location.href=\'/signalflow/item/' + id + '\'">' +
            '<div>' + signalBadge +
            '<span class="iq-signalflow__item-card__time">' + timeStr + '</span></div>' +
            '<div style="flex:1"><div class="iq-signalflow__item-card__title">' + escapeHtml(title || '') + '</div>' +
            (reasoning ? '<div class="iq-signalflow__item-card__reasoning">' + escapeHtml(reasoning) + '</div>' : '') +
            '</div></div>' +
            '<div class="iq-signalflow__item-card__actions">' +
            '<button class="btn btn-outline-primary btn-sm" data-iq-action="reclassify" data-iq-item-id="' + id + '" title="Reclassify">&#9998;</button>' +
            (isNoise ? '<button class="btn btn-outline-success btn-sm" data-iq-action="mark-not-noise" data-iq-item-id="' + id + '" title="Mark not noise">&#10003;</button>' : '') +
            '<button class="btn btn-outline-warning btn-sm" data-iq-action="retry" data-iq-item-id="' + id + '" title="Retry">&#8635;</button>' +
            '</div></div>';
    }

    function escapeHtml(str) {
        if (!str) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(str));
        return div.innerHTML;
    }

    function updateRecentItems(items) {
        var container = document.getElementById('iq-recent-items');
        if (!container || !items) return;
        container.innerHTML = items.map(function (i) {
            return buildItemCardHtml(i.id, i.title, i.signal, i.isNoise, i.reasoning, i.classifiedAt);
        }).join('');
    }

    function prependItemCard(evt) {
        var container = document.getElementById('iq-recent-items');
        if (!container) return;
        var html = buildItemCardHtml(evt.contentId, evt.title, evt.signal, evt.isNoise, evt.reasoning);
        container.insertAdjacentHTML('afterbegin', html);

        var cards = container.querySelectorAll('.iq-signalflow__item-card');
        if (cards.length > 20) {
            cards[cards.length - 1].remove();
        }

        if (evt.isNoise) {
            var noiseLink = document.querySelector('.iq-signalflow__sidebar a[href*="noise"]');
            if (noiseLink) {
                var m = noiseLink.textContent.match(/\d+/);
                noiseLink.textContent = '\u26A0 Noise (' + (m ? parseInt(m[0], 10) + 1 : 1) + ')';
            }
        } else {
            incrementSidebarCount(evt.signal);
        }
    }

    // --- Reclassification modal ---
    function showReclassifyModal(itemId) {
        if (!modal || !signalSelect) return;
        reclassifyItemId = itemId;
        signalSelect.innerHTML = '';
        for (const name of currentSignalNames) {
            const opt = document.createElement('option');
            opt.value = name;
            opt.textContent = name;
            signalSelect.appendChild(opt);
        }
        modal.show();
    }

    function closeReclassifyModal() {
        if (modal) modal.hide();
        reclassifyItemId = null;
    }

    if (confirmBtn && modalEl) {
        confirmBtn.addEventListener('click', function () {
            const signal = signalSelect ? signalSelect.value : '';
            if (!reclassifyItemId || !signal) return;
            connection.invoke('ReclassifyItem', reclassifyItemId, signal, false)
                .then(function () {
                    closeReclassifyModal();
                    updateDashboard();
                    removeItemCard(reclassifyItemId);
                })
                .catch(function (err) { alert('Reclassify failed: ' + err); });
        });
    }

    if (noiseBtn && modalEl) {
        noiseBtn.addEventListener('click', function () {
            if (!reclassifyItemId) return;
            connection.invoke('ReclassifyItem', reclassifyItemId, '', true)
                .then(function () {
                    closeReclassifyModal();
                    updateDashboard();
                    removeItemCard(reclassifyItemId);
                })
                .catch(function (err) { alert('Mark as noise failed: ' + err); });
        });
    }

    function removeItemCard(itemId) {
        const card = document.querySelector('[data-iq-item-id="' + itemId + '"]');
        if (card) {
            const itemCard = card.closest('.iq-signalflow__item-card');
            if (itemCard) itemCard.remove();
        }
    }

    // --- Unified pipeline event handler ---
    function handlePipelineEvent(evt) {
        switch (evt.type) {
            case 'started':
                if (statusEl) statusEl.textContent = 'Running\u2026 ' + evt.totalFeeds + ' feeds';
                if (progressEl) { progressEl.style.display = 'block'; }
                appendLog('Pipeline started with ' + evt.totalFeeds + ' feeds', 'info');
                break;

            case 'progress':
                if (statusEl) statusEl.textContent = evt.stage + ': ' + evt.current + '/' + evt.total;
                if (progressEl) {
                    const bar = progressEl.querySelector('.progress-bar');
                    if (bar) {
                        const pct = evt.total > 0 ? (evt.current / evt.total * 100) : 0;
                        bar.style.width = pct + '%';
                        bar.setAttribute('aria-valuenow', pct);
                    }
                }
                if (evt.message) appendLog(evt.message, 'info');
                break;

            case 'itemprocessed':
                const cls = evt.isNoise ? 'warn' : 'success';
                appendLog('[' + evt.signalOrNoise + '] ' + evt.title, cls);
                prependItemCard(evt);
                break;

            case 'failed':
                appendLog('FAIL [' + evt.stage + '] ' + evt.error, 'error');
                break;

            case 'completed':
                if (statusEl) statusEl.textContent = 'Idle';
                if (progressEl) { progressEl.style.display = 'none'; }
                appendLog('Done: ' + evt.totalItems + ' items, ' + evt.signalCount + ' signals, ' + evt.noiseCount + ' noise, ' + evt.failedCount + ' failed', 'success');
                if (pipelineBtn) { pipelineBtn.disabled = false; pipelineBtn.textContent = '\u25B6 Run Pipeline'; }
                updateDashboard();
                break;
        }
    }

    connection.on('pipelineEvent', handlePipelineEvent);

    // --- Dashboard data handler ---
    connection.on('dashboardData', function (data) {
        currentSignalNames = data.signalNames || [];
        updateSidebar(data.signalCounts || {});
        updateSidebarLink('.iq-signalflow__sidebar a[href*="noise"]', '\u26A0 Noise (' + (data.noiseCount || 0) + ')');
        updateSidebarLink('.iq-signalflow__sidebar a[href*="bounced"]', '\u21BB Bounced (' + (data.bouncedCount || 0) + ')');
        updateRecentItems(data.recentItems);
    });

    // --- Reconnection: restore pipeline state if it was left running ---
    connection.on('pipelineState', function (state) {
        if (state.isRunning) {
            if (pipelineBtn) { pipelineBtn.disabled = true; pipelineBtn.textContent = 'Running\u2026'; }
            if (statusEl) statusEl.textContent = 'Running\u2026';
            if (progressEl) progressEl.style.display = 'block';
            appendLog('Pipeline is running\u2026', 'info');
            if (state.lastEvent) {
                handlePipelineEvent(state.lastEvent);
            }
            updateDashboard();
        }
    });

    // --- Reconnected after drop ---
    connection.onreconnected(function () {
        connection.invoke('GetPipelineState').catch(function (err) {
            console.error('GetPipelineState failed:', err);
        });
        updateDashboard();
    });

    // --- Connection start ---
    connection.start().then(function () {
        connection.invoke('GetPipelineState').catch(function (err) {
            console.error('GetPipelineState failed:', err);
        });
        updateDashboard();
    }).catch(function (err) {
        console.error('SignalR connection error:', err);
    });

    // --- bfcache: page restored from back-forward cache (no script re-execution) ---
    window.addEventListener('pageshow', function () {
        if (connection.state === signalR.HubConnectionState.Disconnected) {
            connection.start().then(function () {
                connection.invoke('GetPipelineState').catch(console.error);
                updateDashboard();
            }).catch(console.error);
        } else if (connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke('GetPipelineState').catch(console.error);
            updateDashboard();
        }
    });

    // --- Pipeline trigger ---
    if (pipelineBtn) {
        pipelineBtn.addEventListener('click', function () {
            pipelineBtn.disabled = true;
            pipelineBtn.textContent = 'Running\u2026';
            if (logEl) logEl.innerHTML = '';
            if (statusEl) statusEl.textContent = 'Starting\u2026';
            connection.invoke('RunPipeline').catch(function (err) {
                console.error('RunPipeline failed:', err);
                appendLog('Failed to start pipeline: ' + err, 'error');
                pipelineBtn.disabled = false;
                pipelineBtn.textContent = '\u25B6 Run Pipeline';
            });
        });
    }

    // --- Item action buttons (reclassify, mark not noise, retry) ---
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('[data-iq-action]');
        if (!btn) return;

        e.stopPropagation();

        const action = btn.getAttribute('data-iq-action');
        const itemId = btn.getAttribute('data-iq-item-id');
        if (!itemId) return;

        if (action === 'reclassify') {
            showReclassifyModal(itemId);
        } else if (action === 'mark-not-noise') {
            connection.invoke('MarkNotNoise', itemId)
                .then(function () {
                    updateDashboard();
                    removeItemCard(itemId);
                })
                .catch(function (err) { alert('Failed: ' + err); });
        } else if (action === 'retry') {
            connection.invoke('RetryItem', itemId)
                .then(function (success) {
                    updateDashboard();
                    removeItemCard(itemId);
                    if (!success) appendLog('Retry limit reached for this item', 'warn');
                })
                .catch(function (err) { alert('Retry failed: ' + err); });
        }
    });
})();
