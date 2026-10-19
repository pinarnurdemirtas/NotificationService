"use strict";
importScripts("https://www.gstatic.com/firebasejs/12.8.0/firebase-app-compat.js");
importScripts("https://www.gstatic.com/firebasejs/12.8.0/firebase-messaging-compat.js");
importScripts("/development/firebase-config.js");

firebase.initializeApp(self.firebaseWebSettings.firebaseConfig);
firebase.messaging().onBackgroundMessage(async payload => {
  // Notification payloads are displayed by Firebase automatically; do not show a duplicate.
  const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
  for (const client of windows) client.postMessage({ type: "firebase-background-message", payload });
});
