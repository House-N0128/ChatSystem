/**
 * ChatSystem Web — 模块化聊天前端
 * 依赖：SignalR (signalr.min.js)、Bootstrap 5 CSS
 */
var ChatApp = (function() {
'use strict';

// ==================== 常量 ====================
var TABS = { PRIVATE: 'private', GROUP: 'group' };
var EMOJI = [
    ['😀','😃','😄','😁','😅','😂','🤣','😊','😍','🥰','😘','😜','🤔','😏','😌','😴'],
    ['👍','👎','👏','🙌','💪','🤝','✌️','🤞','👋','🙏','💀','🤡'],
    ['❤️','🧡','💛','💚','💙','💜','🖤','💔','💯','✅','❌','⭐','🔥','🎉','💡','💬'],
    ['🐱','🐶','🐼','🐨','🐸','🦊','🌸','🌺','🌞','🌈','🍀','🌙'],
    ['🎂','🍰','☕','🍺','🎵','🎮','📱','💻','🚀','✈️','🏠','💰']
];
var EMOJI_NAMES = ['笑脸','手势','爱心','动物','物品'];

// ==================== 状态 ====================
var me = null;
var peerId = null;
var groupId = null;
var groupInfo = null;
var hub = null;
var friends = [];
var groups = [];
var online = {};
var unread = {}; // { uid: count } 未读私聊计数
var unreadGroup = {}; // { gid: count } 未读群聊计数
var tab = TABS.PRIVATE;
var typingTimer = null;

// ==================== DOM 引用 ====================
var $ = function(id) { return document.getElementById(id); };
var dom = {};

// ==================== 工具 ====================
function authHeaders() {
    return { 'Authorization': 'Bearer ' + getToken(), 'Content-Type': 'application/json' };
}
function getToken() {
    var c = document.cookie.split('; ').find(function(r) { return r.startsWith('AuthToken='); });
    return c ? c.split('=')[1] : null;
}
function esc(text) {
    var d = document.createElement('div');
    d.textContent = text;
    return d.innerHTML;
}
function fmtTime(dt) {
    return new Date(dt).toLocaleTimeString();
}
function fmtDate(dt) {
    return new Date(dt).toLocaleString();
}

// ==================== API 调用 ====================
function api(url, method, body) {
    var opts = { method: method || 'GET', headers: authHeaders() };
    if (body) opts.body = JSON.stringify(body);
    return fetch(url, opts).then(function(r) { return r.json(); });
}

// ==================== JWT 解析 ====================
function jwtPayload() {
    var token = getToken();
    if (!token) return null;
    var parts = token.split('.');
    if (parts.length !== 3) return null;
    var base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    var binary = atob(base64);
    var bytes = new Uint8Array(binary.length);
    for (var i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
    return JSON.parse(new TextDecoder('utf-8').decode(bytes));
}
// 从 JWT payload 中提取值，兼容短名和全 URI 两种 key
function jv(payload, shortKey, longKey) {
    return payload[shortKey] || payload[longKey] || '';
}

// ==================== 初始化 ====================
function init() {
    var payload = jwtPayload();
    if (!payload) { location.href = '/account/login'; return; }

    var uid = parseInt(jv(payload, 'nameid', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'));
    var uname = jv(payload, 'unique_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name');
    var nn = payload.nickname || '';
    var role = parseInt(jv(payload, 'role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'));
    if (!uid) { location.href = '/account/login'; return; }
    me = { id: uid, username: uname, nickname: nn, role: role };

    // DOM 引用
    var ids = ['contactList','chatHeader','messageList','inputArea','msgInput','typingHint',
               'fileInput','fileUploadBtn','emojiPopup','emojiBtn','tabPrivate','tabGroup'];
    ids.forEach(function(k) { dom[k] = $(k); });

    // SignalR
    hub = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/chat', { accessTokenFactory: function() { return getToken(); } })
        .withAutomaticReconnect()
        .build();

    hub.on('ReceivePrivateMessage', onPrivateMsg);
    hub.on('ReceiveGroupMessage', onGroupMsg);
    hub.on('UserOnline', function(u) { online[u.id] = true; updateDot(u.id, true); });
    hub.on('UserOffline', function(id) { delete online[id]; updateDot(id, false); });
    hub.on('UserTyping', function(id) { if (tab === TABS.PRIVATE && id === peerId) showTyping(true); });
    hub.on('UserStopTyping', function(id) { if (tab === TABS.PRIVATE && id === peerId) showTyping(false); });
    hub.on('GroupDissolved', onGroupGone);
    hub.on('GroupMemberAdded', onMemberChange);
    hub.on('GroupMemberRemoved', onMemberChange);
    hub.on('UserBanned', onBanned);
    hub.on('MessageDeleted', onMsgDeleted);

    hub.start().then(function() { loadFriends(); loadGroups(); }).catch(function(e) { console.error(e); });

    // 键盘 & 点击事件
    dom.msgInput.addEventListener('keydown', function(e) {
        if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); sendMsg(); }
    });
    dom.msgInput.addEventListener('input', function() {
        if (tab === TABS.PRIVATE && peerId) {
            hub.invoke('NotifyTyping', peerId);
            clearTimeout(typingTimer);
            typingTimer = setTimeout(function() { hub.invoke('NotifyStopTyping', peerId); }, 2000);
        }
    });
    document.addEventListener('click', function(e) {
        var pop = dom.emojiPopup;
        if (pop && pop.style.display === 'block' && !pop.contains(e.target) && e.target !== dom.emojiBtn) {
            pop.style.display = 'none';
        }
    });

    // 如果当前页面是聊天页（有标签页）初始化
    if (dom.tabPrivate) switchTab(TABS.PRIVATE);
}

// ==================== 标签切换 ====================
function switchTab(t) {
    tab = t;
    peerId = null; groupId = null; groupInfo = null;
    if (dom.tabPrivate) dom.tabPrivate.classList.toggle('active', t === TABS.PRIVATE);
    if (dom.tabGroup) dom.tabGroup.classList.toggle('active', t === TABS.GROUP);
    header('👋 请选择' + (t === TABS.PRIVATE ? '好友' : '群组') + '开始聊天', '');
    dom.messageList.innerHTML = '';
    dom.inputArea.style.display = 'none';
    dom.emojiPopup.style.display = 'none';
    if (t === TABS.PRIVATE) renderFriends();
    else renderGroups();
}

window.switchTab = switchTab;

// 只更新单个好友的在线圆点，避免全量 innerHTML 导致闪烁
function updateDot(uid, isOnline) {
    var el = document.querySelector('[data-uid="' + uid + '"] .dot');
    if (el) { el.classList.toggle('online', isOnline); el.classList.toggle('offline', !isOnline); }
}

// ==================== 好友列表 ====================
function loadFriends() {
    api('/api/friends').then(function(d) {
        if (d.success) { friends = d.data; d.data.forEach(function(f) { if (f.isOnline) online[f.friendUserId] = true; }); renderFriends(); }
    });
}

function renderFriends() {
    if (tab !== TABS.PRIVATE || !dom.contactList) return;
    var h = '<h6 class="mb-3">好友列表</h6>';
    if (friends.length === 0) { h += '<p class="text-muted small">暂无好友，去<a href="/chat/friends">添加好友</a></p>'; }
    else {
        friends.forEach(function(f) {
            var on = online[f.friendUserId];
            var cnt = unread[f.friendUserId] || 0;
            h += '<div class="contact-item" onclick="ChatApp.openChat(' + f.friendUserId + ',\'' + esc(f.friendNickname) + '\')" data-uid="' + f.friendUserId + '">';
            h += '<span class="dot ' + (on ? 'online' : 'offline') + '"></span>';
            h += '<span>' + esc(f.friendNickname) + '</span>';
            h += '<span class="unread-badge" style="display:' + (cnt > 0 ? '' : 'none') + '">' + cnt + '</span>';
            h += '</div>';
        });
    }
    dom.contactList.innerHTML = h;
}

// ==================== 群组列表 ====================
function loadGroups() {
    api('/api/groups').then(function(d) {
        if (d.success) { groups = d.data; renderGroups(); }
    });
}

function renderGroups() {
    if (tab !== TABS.GROUP || !dom.contactList) return;
    var h = '<h6 class="mb-3">我的群组</h6>';
    if (groups.length === 0) { h += '<p class="text-muted small">暂无群组</p>'; }
    else {
        groups.forEach(function(g) {
            var gcnt = unreadGroup[g.id] || 0;
            h += '<div class="contact-item" onclick="ChatApp.openGroup(' + g.id + ',\'' + esc(g.name) + '\',' + g.creatorId + ',\'' + esc(g.creatorName) + '\')" data-gid="' + g.id + '">';
            h += '<span>👥 ' + esc(g.name) + ' (' + g.memberCount + '人)</span>';
            h += '<span class="unread-badge" style="display:' + (gcnt > 0 ? '' : 'none') + '">' + gcnt + '</span>';
            h += '</div>';
        });
    }
    h += '<div class="create-group-row">';
    h += '<input type="text" id="newGroupName" placeholder="群名称" class="form-control form-control-sm">';
    h += '<button class="btn btn-sm btn-primary" onclick="ChatApp.createGroup()">创建</button>';
    h += '</div>';
    dom.contactList.innerHTML = h;
}

function createGroup() {
    var el = document.getElementById('newGroupName');
    var name = (el ? el.value : '').trim();
    if (!name) return alert('请输入群名称');
    api('/api/groups', 'POST', { name: name, memberIds: [] }).then(function(d) {
        if (d.success) { loadGroups(); if (el) el.value = ''; } else alert(d.message);
    });
}

// ==================== 打开聊天 ====================
function openChat(uid, name) {
    tab = TABS.PRIVATE; peerId = uid; groupId = null; groupInfo = null;
    unread[uid] = 0; updateBadge(uid, 0, 'uid');
    header('💬 ' + name, '');
    dom.inputArea.style.display = 'flex';
    dom.fileUploadBtn.style.display = 'inline-flex';
    dom.messageList.innerHTML = '';
    highlightContact('uid', uid);
    loadHistory();
}

function openGroup(gid, name, creatorId, creatorName) {
    tab = TABS.GROUP; groupId = gid; peerId = null;
    unreadGroup[gid] = 0; updateBadge(gid, 0, 'gid');
    groupInfo = { id: gid, name: name, creatorId: creatorId, creatorName: creatorName };
    var isCreator = creatorId === me.id;
    var btns = isCreator
        ? '<button class="btn btn-outline-danger btn-sm" onclick="ChatApp.dissolveGroup()">解散</button><button class="btn btn-outline-primary btn-sm" onclick="ChatApp.inviteMember()">＋邀请</button>'
        : '<button class="btn btn-outline-secondary btn-sm" onclick="ChatApp.leaveGroup()">退出</button>';
    header('👥 ' + name, btns);
    dom.inputArea.style.display = 'flex';
    dom.fileUploadBtn.style.display = 'inline-flex';
    dom.messageList.innerHTML = '';
    highlightContact('gid', gid);
    loadGroupHistory();
    hub.invoke('JoinGroup', gid).catch(function(){});
}

function highlightContact(attr, val) {
    document.querySelectorAll('.contact-item').forEach(function(el) { el.classList.toggle('active', parseInt(el.dataset[attr]) === val); });
}

function header(title, btns) {
    dom.chatHeader.innerHTML = '<span class="title">' + title + '</span>' + (btns ? '<span class="btns">' + btns + '</span>' : '');
}

// ==================== 发送消息 ====================
function sendMsg() {
    var text = dom.msgInput.value.trim();
    if (!text) return;
    if (!hub || hub.state !== signalR.HubConnectionState.Connected) { alert('正在连接服务器，请稍后重试'); return; }
    dom.msgInput.value = '';

    // 本地回显 —— 不等待服务器广播，立刻显示（用临时 ID，后续服务器播回时替换为真实 ID）
    if (tab === TABS.PRIVATE && peerId) {
        var localMsg = { id: 'local' + Date.now(), senderId: me.id, senderNickname: me.nickname, receiverId: peerId, content: text, sentAt: new Date().toISOString(), messageType: 0 };
        appendMsg(localMsg);
        hub.invoke('SendPrivateMessage', peerId, text).catch(function(e) {
            console.error('发送失败:', e);
            alert('消息发送失败，请检查网络连接');
        });
    } else if (tab === TABS.GROUP && groupId) {
        var localGroupMsg = { id: 'local' + Date.now(), groupId: groupId, senderId: me.id, senderNickname: me.nickname, content: text, sentAt: new Date().toISOString(), messageType: 0 };
        appendGroupMsg(localGroupMsg);
        hub.invoke('SendGroupMessage', groupId, text).catch(function(e) {
            console.error('发送失败:', e);
            alert('消息发送失败，请检查网络连接');
        });
    }
    if (tab === TABS.PRIVATE && peerId) hub.invoke('NotifyStopTyping', peerId).catch(function(){});
    showTyping(false);
}

// ==================== 接收消息 ====================
function onPrivateMsg(msg) {
    var rel = msg.senderId === me.id ? msg.receiverId : msg.senderId;
    // 不是当前聊天对象 → 累加未读计数
    if (tab !== TABS.PRIVATE || peerId !== rel) {
        if (msg.senderId !== me.id) {
            unread[msg.senderId] = (unread[msg.senderId] || 0) + 1;
            updateBadge(msg.senderId, unread[msg.senderId], 'uid');
        }
        return;
    }
    if (msg.senderId === me.id) {
        replaceLocalEcho(msg);
        return;
    }
    appendMsg(msg);
}

function onGroupMsg(msg) {
    // 不是当前群 → 累加未读
    if (tab !== TABS.GROUP || groupId !== msg.groupId) {
        if (msg.senderId !== me.id) {
            unreadGroup[msg.groupId] = (unreadGroup[msg.groupId] || 0) + 1;
            updateBadge(msg.groupId, unreadGroup[msg.groupId], 'gid');
        }
        return;
    }
    if (msg.senderId === me.id) {
        replaceLocalEcho(msg);
        return;
    }
    appendGroupMsg(msg);
}

// 更新好友/群组列表项上的红点徽章
function updateBadge(id, count, attr) {
    var el = document.querySelector('[data-' + attr + '="' + id + '"] .unread-badge');
    if (el) {
        if (count > 0) { el.textContent = count; el.style.display = ''; }
        else el.style.display = 'none';
    }
}

// 找到最近一个本地回显气泡，用服务器返回的真实消息替换
function replaceLocalEcho(msg) {
    var locals = dom.messageList.querySelectorAll('[data-msgid^="local"]');
    if (locals.length > 0) {
        var last = locals[locals.length - 1];
        last.outerHTML = buildMsgHtml(msg, msg.groupId || 0);
    }
}

function appendMsg(msg) {
    dom.messageList.insertAdjacentHTML('beforeend', buildMsgHtml(msg, 0));
    dom.messageList.scrollTop = dom.messageList.scrollHeight;
}

function buildMsgHtml(msg, gid) {
    var mine = msg.senderId === me.id;
    var extra = gid ? ' data-gid="' + gid + '"' : '';
    var h = '<div class="msg-bubble ' + (mine ? 'mine' : 'other') + '" data-msgid="' + msg.id + '"' + extra + '>';
    if (!mine) h += '<div class="sender">' + esc(msg.senderNickname) + '</div>';
    var isFile = msg.messageType === 1;
    if (isFile && msg.filePath) {
        h += '<div class="content"><a href="' + msg.filePath + '" target="_blank" style="color:inherit;text-decoration:underline;">📎 ' + esc(msg.fileName || '文件') + '</a></div>';
    } else {
        h += '<div class="content">' + esc(msg.content) + '</div>';
    }
    h += '<div class="time">' + fmtTime(msg.sentAt);
    if (mine) h += '<button class="del-btn" onclick="ChatApp.deleteMsg(' + msg.id + (gid ? ',' + gid : '') + ')">删除</button>';
    h += '</div></div>';
    return h;
}

function appendGroupMsg(msg) {
    dom.messageList.insertAdjacentHTML('beforeend', buildMsgHtml(msg, msg.groupId || groupId));
    dom.messageList.scrollTop = dom.messageList.scrollHeight;
}

// ==================== 历史消息 ====================
function loadHistory() {
    api('/api/messages/private/' + peerId + '?page=1&pageSize=50').then(function(d) {
        if (!d.success || !d.data) return;
        var html = '';
        d.data.items.slice().reverse().forEach(function(m) { html += buildMsgHtml(m); });
        dom.messageList.innerHTML = html;
        dom.messageList.scrollTop = dom.messageList.scrollHeight;
    });
}

function loadGroupHistory() {
    api('/api/groups/' + groupId + '/messages?page=1&pageSize=50').then(function(d) {
        if (!d.success || !d.data) return;
        var html = '';
        d.data.forEach(function(m) { html += buildMsgHtml(m, groupId); });
        dom.messageList.innerHTML = html;
        dom.messageList.scrollTop = dom.messageList.scrollHeight;
    });
}

// ==================== 消息操作 ====================
function deleteMsg(id, gid) {
    if (!confirm('确定删除此消息？')) return;
    var url = gid ? '/api/groups/' + gid + '/messages/' + id : '/api/messages/' + id;
    api(url, 'DELETE').then(function(d) {
        if (d.success) { var el = document.querySelector('[data-msgid="' + id + '"]'); if (el) el.remove(); }
        else alert(d.message);
    });
}

function onMsgDeleted(id) {
    var el = document.querySelector('[data-msgid="' + id + '"]');
    if (el) el.remove();
}

// ==================== 文件上传 ====================
function uploadFile() {
    var fi = dom.fileInput;
    var file = fi.files[0];
    if (!file) return;
    var fd = new FormData();
    fd.append('file', file);
    var url, onOk;
    if (tab === TABS.PRIVATE && peerId) {
        fd.append('receiverId', peerId);
        url = '/api/messages/file';
        onOk = function(d) { appendMsg(d.data); };
    } else if (tab === TABS.GROUP && groupId) {
        fd.append('groupId', groupId);
        url = '/api/groups/file';
        onOk = function(d) { appendGroupMsg(d.data); };
    } else { fi.value = ''; return; }
    fetch(url, { method: 'POST', headers: { 'Authorization': 'Bearer ' + getToken() }, body: fd })
        .then(function(r) { return r.json(); }).then(function(d) { if (d.success) onOk(d); else alert(d.message); })
        .catch(function() { alert('上传失败'); });
    fi.value = '';
}

// ==================== 群管理 ====================
function dissolveGroup() {
    if (!groupId || !confirm('确定解散群聊「' + groupInfo.name + '」？此操作不可恢复。')) return;
    api('/api/groups/' + groupId, 'DELETE').then(function(d) {
        if (d.success) { clearGroupChat(); loadGroups(); } else alert(d.message);
    });
}

function leaveGroup() {
    if (!groupId || !confirm('确定退出群聊「' + groupInfo.name + '」？')) return;
    api('/api/groups/' + groupId + '/members/' + me.id, 'DELETE').then(function(d) {
        if (d.success) { hub.invoke('LeaveGroup', groupId).catch(function(){}); clearGroupChat(); loadGroups(); }
        else alert(d.message);
    });
}

function inviteMember() {
    if (!groupId) return;
    api('/api/groups/' + groupId).then(function(d) {
        if (!d.success || !d.data) return;
        var memberIds = d.data.members.map(function(m) { return m.userId; });
        var avail = friends.filter(function(f) { return memberIds.indexOf(f.friendUserId) === -1; });
        if (avail.length === 0) { alert('所有好友都已在群中'); return; }
        var list = avail.map(function(f) {
            return '<label style="display:block;margin:4px 0;cursor:pointer"><input type="checkbox" value="' + f.friendUserId + '"> ' + esc(f.friendNickname) + ' (' + esc(f.friendUsername) + ')</label>';
        }).join('');
        var html = '<div style="padding:16px"><h6>邀请好友加入「' + esc(groupInfo.name) + '」</h6>' + list;
        html += '<button class="btn btn-primary btn-sm mt-3" id="inviteOk">确认邀请</button></div>';
        showDialog(html, 'inviteOk', function(dialog) {
            var checks = dialog.querySelectorAll('input:checked');
            var ps = [];
            checks.forEach(function(cb) { ps.push(api('/api/groups/' + groupId + '/members', 'POST', parseInt(cb.value))); });
            Promise.all(ps).then(function() { dialog.remove(); loadGroups(); });
        });
    });
}

function clearGroupChat() {
    groupId = null; groupInfo = null;
    header('👋 请选择群组开始聊天', '');
    dom.messageList.innerHTML = '';
    dom.inputArea.style.display = 'none';
}

// ==================== 群事件 ====================
function onGroupGone(gid) {
    if (groupId === gid) { clearGroupChat(); }
    loadGroups();
}

function onMemberChange(gid) {
    if (groupId === gid) {
        api('/api/groups/' + gid).then(function(d) {
            if (!d.success || !d.data) return;
            groupInfo = { id: d.data.id, name: d.data.name, creatorId: d.data.creatorId, creatorName: d.data.creatorName };
            var isC = d.data.creatorId === me.id;
            header('👥 ' + d.data.name + ' (' + d.data.memberCount + '人)', isC
                ? '<button class="btn btn-outline-danger btn-sm" onclick="ChatApp.dissolveGroup()">解散</button><button class="btn btn-outline-primary btn-sm" onclick="ChatApp.inviteMember()">＋邀请</button>'
                : '<button class="btn btn-outline-secondary btn-sm" onclick="ChatApp.leaveGroup()">退出</button>');
        });
    }
    loadGroups();
}

function onBanned(msg) {
    alert(msg || '您的账号已被管理员封禁');
    document.cookie = 'AuthToken=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
    location.href = '/account/login';
}

// ==================== Emoji ====================
function toggleEmoji() {
    var p = dom.emojiPopup;
    if (p.style.display === 'block') { p.style.display = 'none'; return; }
    p.style.display = 'block';
    buildEmojiPanel();
    switchEmojiTab(0);
}
function buildEmojiPanel() {
    // 只建一次标签行，后面切换只更新网格
    var tabsHtml = '';
    EMOJI_NAMES.forEach(function(name, i) {
        tabsHtml += '<button class="emoji-tab" data-idx="' + i + '">' + name + '</button>';
    });
    dom.emojiPopup.innerHTML = '<div class="emoji-tabs" id="emojiTabs">' + tabsHtml + '</div><div class="emoji-grid" id="emojiGrid"></div>';
    // 给标签绑定事件（用 addEventListener 不用 onclick）
    var tabs = document.querySelectorAll('#emojiTabs .emoji-tab');
    tabs.forEach(function(t) {
        t.addEventListener('click', function() { switchEmojiTab(parseInt(this.dataset.idx)); });
    });
}
function switchEmojiTab(idx) {
    // 更新标签高亮
    var tabs = document.querySelectorAll('#emojiTabs .emoji-tab');
    tabs.forEach(function(t, i) { t.classList.toggle('active', i === idx); });
    // 只更新网格 HTML
    var grid = document.getElementById('emojiGrid');
    if (!grid) return;
    var h = '';
    EMOJI[idx].forEach(function(e) {
        h += '<button class="emoji-item" data-emoji="' + e + '">' + e + '</button>';
    });
    grid.innerHTML = h;
    // 用 addEventListener 绑定，不走 onclick 字符串
    grid.querySelectorAll('.emoji-item').forEach(function(btn) {
        btn.addEventListener('click', function() {
            insertEmoji(this.dataset.emoji);
        });
    });
}

function insertEmoji(emoji) {
    var inp = dom.msgInput;
    var s = inp.selectionStart, e = inp.selectionEnd;
    inp.value = inp.value.substring(0, s) + emoji + inp.value.substring(e);
    inp.selectionStart = inp.selectionEnd = s + emoji.length;
    inp.focus();
    dom.emojiPopup.style.display = 'none';
}

// ==================== 打字状态 ====================
function showTyping(show) { dom.typingHint.style.display = show ? 'block' : 'none'; dom.typingHint.textContent = '对方正在输入...'; }

// ==================== 弹窗工具 ====================
function showDialog(html, btnId, onConfirm) {
    var dialog = document.createElement('div');
    dialog.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.45);z-index:9999;display:flex;align-items:center;justify-content:center';
    dialog.innerHTML = '<div style="background:#fff;border-radius:12px;max-width:380px;width:90%;max-height:80vh;overflow-y:auto">' + html + '</div>';
    dialog.addEventListener('click', function(e) { if (e.target === dialog) dialog.remove(); });
    document.body.appendChild(dialog);
    var btn = dialog.querySelector('#' + btnId);
    if (btn) btn.addEventListener('click', function() { onConfirm(dialog); });
}

// 子页面保活：连接 SignalR 防止被判定离线，不处理聊天消息
function keepAlive() {
    if (hub) return;
    hub = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/chat', { accessTokenFactory: function() { return getToken(); } })
        .withAutomaticReconnect()
        .build();
    hub.on('UserBanned', onBanned);
    hub.start().catch(function(){});
}

// ==================== 好友管理页 ====================
function initFriendsPage() {
    var payload = jwtPayload();
    if (!payload) { location.href = '/account/login'; return; }
    var uid = parseInt(jv(payload, 'nameid', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'));
    if (!uid) { location.href = '/account/login'; return; }
    me = { id: uid, username: jv(payload, 'unique_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'), nickname: payload.nickname || '', role: parseInt(jv(payload, 'role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role')) };
    keepAlive();
    loadFriendList();
}

function loadFriendList() {
    api('/api/friends').then(function(d) {
        if (d.success) { friends = d.data; d.data.forEach(function(f) { if (f.isOnline) online[f.friendUserId] = true; }); renderFriendList(); }
    });
}

function renderFriendList() {
    var el = $('friendList');
    if (!el) return;
    var h = '';
    if (friends.length === 0) h = '<p class="text-muted">暂无好友</p>';
    else {
        friends.forEach(function(f) {
            var on = online[f.friendUserId];
            h += '<div class="friend-row"><div class="info"><span class="dot ' + (on ? 'online' : 'offline') + '"></span>';
            h += '<strong>' + esc(f.friendNickname) + '</strong> <small class="text-muted">@' + esc(f.friendUsername) + '</small></div>';
            h += '<button class="btn btn-outline-danger btn-sm" onclick="ChatApp.removeFriend(' + f.friendUserId + ')">删除</button></div>';
        });
    }
    el.innerHTML = h;
}

function searchUsers() {
    var kw = ($('searchInput') || {}).value;
    if (!kw || !kw.trim()) return;
    api('/api/users/search?keyword=' + encodeURIComponent(kw.trim())).then(function(d) {
        var el = $('searchResults');
        if (!el) return;
        var h = '';
        if (!d.success || !d.data || d.data.length === 0) { h = '<p class="text-muted">未找到用户</p>'; }
        else {
            d.data.forEach(function(u) {
                var already = friends.some(function(f) { return f.friendUserId === u.id; });
                h += '<div class="result-item"><div><strong>' + esc(u.nickname) + '</strong> <small class="text-muted">@' + esc(u.username) + '</small></div>';
                h += already ? '<span class="text-muted small">已是好友</span>' : '<button class="btn btn-outline-primary btn-sm" onclick="ChatApp.sendRequest(' + u.id + ')">添加好友</button>';
                h += '</div>';
            });
        }
        el.innerHTML = h;
    });
}

function sendRequest(uid) {
    api('/api/friends/request', 'POST', { toUserId: uid }).then(function(d) {
        alert(d.message);
        if (d.success) { loadFriendList(); searchUsers(); }
    });
}

function removeFriend(fid) {
    if (!confirm('确定删除该好友？')) return;
    api('/api/friends/' + fid, 'DELETE').then(function(d) {
        alert(d.message);
        if (d.success) loadFriendList();
    });
}

// ==================== 历史记录页 ====================
function initHistoryPage() {
    var payload = jwtPayload();
    if (!payload) { location.href = '/account/login'; return; }
    var uid = parseInt(jv(payload, 'nameid', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'));
    if (!uid) { location.href = '/account/login'; return; }
    me = { id: uid, username: jv(payload, 'unique_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'), nickname: payload.nickname || '', role: parseInt(jv(payload, 'role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role')) };
    keepAlive();
    // 绑定实时搜索：输入关键词或改日期即搜
    var kwEl = $('histKeyword'), fromEl = $('histFrom'), toEl = $('histTo');
    if (kwEl) kwEl.addEventListener('input', debounce(searchHistory, 300));
    if (fromEl) fromEl.addEventListener('change', searchHistory);
    if (toEl) toEl.addEventListener('change', searchHistory);
}

var _searchTimer = null;
function debounce(fn, ms) {
    return function() { clearTimeout(_searchTimer); _searchTimer = setTimeout(fn, ms); };
}

function searchHistory() {
    var kw = ($('histKeyword') || {}).value || '';
    var from = ($('histFrom') || {}).value || '';
    var to = ($('histTo') || {}).value || '';
    var url = '/api/messages/history?page=1&pageSize=50';
    if (kw) url += '&keyword=' + encodeURIComponent(kw);
    api(url).then(function(d) {
        var el = $('histResults');
        if (!el) return;
        if (!d.success || !d.data || d.data.items.length === 0) { el.innerHTML = '<p class="text-muted">未找到消息</p>'; return; }
        // 客户端日期过滤
        var items = d.data.items;
        if (from) { var fd = new Date(from); items = items.filter(function(m) { return new Date(m.sentAt) >= fd; }); }
        if (to)   { var td = new Date(to + 'T23:59:59'); items = items.filter(function(m) { return new Date(m.sentAt) <= td; }); }
        if (items.length === 0) { el.innerHTML = '<p class="text-muted">未找到消息</p>'; return; }
        var h = '<p class="text-muted small mb-2">共 ' + items.length + ' 条结果</p>';
        items.forEach(function(m) {
            var isMine = m.senderId === me.id;
            var isGroup = m.chatType === 'group';
            var direction = '';
            if (isGroup) {
                direction = '在 <strong>' + esc(m.groupName) + '</strong> 中 ';
                direction += isMine ? '我说' : esc(m.senderNickname) + '说';
            } else {
                direction = isMine ? '发送给 <strong>' + esc(m.partnerNickname) + '</strong>' : '来自 <strong>' + esc(m.senderNickname) + '</strong>';
            }
            h += '<div class="msg-row" data-msgid="' + m.id + (isGroup ? '" data-gid="' + (m.groupId||0) : '') + '">';
            h += '<div><div style="font-size:13px">' + direction + '</div>';
            h += '<div>' + esc(m.content) + '</div><div class="meta">' + fmtDate(m.sentAt) + '</div></div>';
            if (isMine) {
                var delArgs = m.id + (isGroup ? ',' + (m.groupId||0) : '');
                h += '<button class="del-btn" onclick="ChatApp.deleteMsg(' + delArgs + ')">删除</button>';
            }
            h += '</div>';
        });
        el.innerHTML = h;
    });
}

// ==================== 导出 ====================
return {
    init: init,
    switchTab: switchTab,
    openChat: openChat,
    openGroup: openGroup,
    sendMsg: sendMsg,
    createGroup: createGroup,
    dissolveGroup: dissolveGroup,
    leaveGroup: leaveGroup,
    inviteMember: inviteMember,
    deleteMsg: deleteMsg,
    toggleEmoji: toggleEmoji,
    switchEmojiTab: switchEmojiTab,
    insertEmoji: insertEmoji,
    uploadFile: uploadFile,
    searchUsers: searchUsers,
    sendRequest: sendRequest,
    removeFriend: removeFriend,
    searchHistory: searchHistory,
    loadFriendList: loadFriendList,
    loadFriends: loadFriends,
    loadGroups: loadGroups,
    initFriendsPage: initFriendsPage,
    initHistoryPage: initHistoryPage
};

})();
