"use strict";
(() => {
  const $ = id => document.getElementById(id);
  let session = null;
  let messaging = null;
  let token = "";
  let usersById = new Map();
  let mode = "login";
  let sending = false;
  let deviceId = sessionStorage.getItem("notification.deviceId");
  const status = (text, error = false) => { $("status").textContent = text; $("status").dataset.error = String(error); };
  const errorText = error => {
    if (window.Notification?.permission === "denied") return "Bildirim izni engelli. Adres çubuğundaki site ayarlarından bildirimlere izin ver.";
    return error?.message || "İşlem tamamlanamadı.";
  };
  function storeSession(value) {
    session = value;
    if (value) sessionStorage.setItem("notification.session", JSON.stringify(value));
    else sessionStorage.removeItem("notification.session");
  }
  function paintAccount() {
    $("signed-out").hidden = !!session;
    $("signed-in").hidden = !session;
    $("workspace").hidden = !session;
    $("account").textContent = session ? `${session.user.displayName} (@${session.user.username}) olarak giriş yaptın.` : "";
    $("register").disabled = !session || !messaging || sending;
    $("send").disabled = !session || !$("recipient").value || sending;
  }
  async function api(path, { method = "GET", body, anonymous = false } = {}) {
    const headers = {};
    if (body !== undefined) headers["Content-Type"] = "application/json";
    if (session && !anonymous) headers.Authorization = `Bearer ${session.accessToken}`;
    const response = await fetch(path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body), cache: "no-store" });
    const text = await response.text();
    let data;
    try { data = text ? JSON.parse(text) : null; } catch { throw new Error(`Sunucu yanıtı okunamadı (HTTP ${response.status}).`); }
    if (!response.ok) {
      const error = new Error(data?.detail || data?.title || data?.results?.find(x => x.notification?.errorMessage)?.notification.errorMessage || (response.status === 401 ? "Oturumun sona erdi. Tekrar giriş yap." : `HTTP ${response.status}`));
      error.status = response.status; error.data = data;
      throw error;
    }
    return data;
  }
  async function loadUsers() {
    const selected = $("recipient").value;
    const users = await api("/api/users");
    usersById = new Map(users.map(user => [user.id, user.displayName]));
    $("recipient").replaceChildren(new Option("Alıcı seç", ""));
    for (const user of users) $("recipient").add(new Option(`${user.displayName} (@${user.username})${user.id === session.user.id ? " — kendim" : ""}`, user.id));
    $("recipient").value = selected;
    paintAccount();
  }
  async function loadHistory() {
    const rows = await api("/api/push-notifications?take=20");
    const host = $("history"); host.replaceChildren();
    if (!rows.length) { host.textContent = "Henüz kayıt yok."; return; }
    for (const row of rows) {
      const item = document.createElement("p");
      const direction = row.senderUserId === session.user.id ? "Gönderilen" : "Alınan";
      item.textContent = `${direction}: ${usersById.get(row.senderUserId) || "Kullanıcı"} → ${usersById.get(row.recipientUserId) || "Kullanıcı"} · ${row.title} · ${row.body} · ${row.status} · ${new Date(row.createdAtUtc).toLocaleString("tr-TR")}`;
      host.append(item);
    }
  }
  const showMessage = payload => {
    const message = payload?.notification || {};
    $("received").textContent = `${new Date().toLocaleTimeString("tr-TR")} · ${message.title || "Bildirim"} — ${message.body || ""}`;
    if (session) loadHistory().catch(() => {});
  };
  navigator.serviceWorker?.addEventListener("message", event => {
    if (event.data?.type === "firebase-background-message" && session) showMessage(event.data.payload);
  });
  function setMode(value) {
    mode = value;
    $("display-name-field").hidden = mode !== "register";
    $("display-name").disabled = mode !== "register";
    $("display-name").required = mode === "register";
    $("password").autocomplete = mode === "register" ? "new-password" : "current-password";
    $("password").minLength = mode === "register" ? 10 : 1;
    $("auth-submit").textContent = mode === "register" ? "Hesap oluştur ve giriş yap" : "Giriş yap";
  }
  $("mode-login").addEventListener("click", () => setMode("login"));
  $("mode-register").addEventListener("click", () => setMode("register"));
  $("auth-form").addEventListener("submit", async event => {
    event.preventDefault(); $("auth-submit").disabled = true;
    try {
      const body = { username: $("username").value.trim(), password: $("password").value };
      if (mode === "register") body.displayName = $("display-name").value.trim();
      const authenticated = await api(`/api/auth/${mode}`, { method: "POST", body, anonymous: true });
      // Device ID belongs to a particular browser/account pair.
      if (sessionStorage.getItem("notification.deviceUserId") !== authenticated.user.id) { deviceId = null; sessionStorage.removeItem("notification.deviceId"); }
      storeSession(authenticated); $("password").value = ""; paintAccount();
      await loadUsers(); await loadHistory();
      status("Giriş başarılı. Bildirim almak için bu cihazı kaydet; göndermek için alıcıyı seç.");
    } catch (error) { status(errorText(error), true); }
    finally { $("auth-submit").disabled = false; }
  });
  $("register").addEventListener("click", async () => {
    $("register").disabled = true;
    try {
      if (!messaging) throw new Error("Bu tarayıcı Firebase push desteklemiyor; Chrome veya Edge ile aç.");
      const permission = await Notification.requestPermission();
      if (permission !== "granted") throw new Error("Bildirim izni verilmedi.");
      status("Tarayıcı token’ı alınıyor ve hesabına bağlanıyor…");
      const registration = await navigator.serviceWorker.register("/firebase-messaging-sw.js", { scope: "/", updateViaCache: "none" });
      await navigator.serviceWorker.ready;
      token = await messaging.getToken({ vapidKey: self.firebaseWebSettings.vapidKey, serviceWorkerRegistration: registration });
      if (!token) throw new Error("Firebase cihaz token’ı üretmedi.");
      const registered = await api("/api/devices", { method: "POST", body: { token, name: $("device-name").value.trim(), targetKind: "FcmToken" } });
      deviceId = registered.id; sessionStorage.setItem("notification.deviceId", deviceId);
      sessionStorage.setItem("notification.deviceUserId", session.user.id);
      $("token").value = token; $("copy").disabled = false;
      status(`Bu tarayıcı ${session.user.displayName} hesabına bağlandı. Artık bu kullanıcıya bildirim gönderilebilir.`);
    } catch (error) { status(errorText(error), true); }
    finally { paintAccount(); }
  });
  $("copy").addEventListener("click", async () => {
    try { await navigator.clipboard.writeText(token); status("Token kopyalandı."); }
    catch { $("token").focus(); $("token").select(); status("Ctrl+C ile kopyala."); }
  });
  $("logout").addEventListener("click", async () => {
    if (sending) return;
    $("logout").disabled = true;
    try {
      if (deviceId) {
        try { await api(`/api/devices/${deviceId}`, { method: "DELETE" }); }
        catch (error) { if (![401,404].includes(error.status)) throw error; }
      }
      if (messaging && window.Notification?.permission === "granted") await messaging.deleteToken();
      storeSession(null); deviceId = null; token = "";
      sessionStorage.removeItem("notification.deviceId");
      sessionStorage.removeItem("notification.deviceUserId");
      $("token").value = ""; $("copy").disabled = true;
      $("result").textContent = "Gönderim sonucu burada görünecek.";
      $("received").textContent = "Henüz canlı bildirim alınmadı.";
      $("recipient").replaceChildren(new Option("Alıcı seç", "")); $("history").replaceChildren();
      paintAccount(); status("Bu tarayıcının bildirim kaydı kaldırıldı ve oturum kapatıldı.");
    } catch (error) { status(errorText(error), true); }
    finally { $("logout").disabled = false; }
  });
  $("recipient").addEventListener("change", paintAccount);
  $("refresh-users").addEventListener("click", () => loadUsers().then(() => status("Kullanıcı listesi yenilendi.")).catch(error => status(errorText(error), true)));
  $("refresh-history").addEventListener("click", () => loadHistory().catch(error => status(errorText(error), true)));
  $("send-form").addEventListener("submit", async event => {
    event.preventDefault();
    if (!session || sending || !$("recipient").value) return;
    sending = true; $("logout").disabled = true; paintAccount();
    try {
      const body = { recipientUserId: $("recipient").value, title: $("title").value, body: $("body").value, data: { source: "user-push-test" } };
      if (Number($("delay").value)) { status("5 saniye sonra gönderilecek. Alıcının tarayıcısını arka plana alabilirsin."); await new Promise(resolve => setTimeout(resolve, 5000)); }
      const result = await api("/api/push-notifications", { method: "POST", body });
      $("result").textContent = JSON.stringify(result, null, 2);
      status(`${result.sentCount} cihazda Firebase kabul etti; ${result.failedCount} başarısız, ${result.unknownCount} belirsiz.`);
      await loadHistory();
    } catch (error) {
      if (error.data) $("result").textContent = JSON.stringify(error.data, null, 2);
      status(errorText(error), true);
    } finally { sending = false; $("logout").disabled = false; paintAccount(); }
  });
  (async () => {
    try {
      if (window.isSecureContext && "Notification" in window && "serviceWorker" in navigator && self.firebase?.messaging && await firebase.messaging.isSupported()) {
        if (!self.firebaseWebSettings?.firebaseConfig?.apiKey || !self.firebaseWebSettings.vapidKey) throw new Error("FirebaseWeb ayarları eksik.");
        firebase.initializeApp(self.firebaseWebSettings.firebaseConfig); messaging = firebase.messaging();
        messaging.onMessage(payload => { if (session) showMessage(payload); });
      }
      const stored = sessionStorage.getItem("notification.session");
      if (stored) {
        const candidate = JSON.parse(stored);
        if (Date.parse(candidate.expiresAtUtc) > Date.now()) {
          storeSession(candidate);
          try { session.user = await api("/api/auth/me"); } catch { storeSession(null); }
        } else sessionStorage.removeItem("notification.session");
      }
      paintAccount();
      if (session) { await loadUsers(); await loadHistory(); }
      status(session ? "Oturum hazır. Alıcıyı seçebilir veya cihazını yeniden kaydedebilirsin." : "Önce giriş yap veya Pınar / Ayşe için ayrı hesap oluştur.");
    } catch (error) { paintAccount(); status(errorText(error), true); }
  })();
})();
