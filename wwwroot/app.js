// ---------- Tiny helpers ----------
const $ = (id) => document.getElementById(id);
let mode = "login";      // "login" or "signup"
let editingId = null;    // id of the task being edited, or null when adding
let tasks = [];

const getToken = () => localStorage.getItem("token");

// One wrapper for every API call: adds the JWT, parses JSON, throws readable errors
async function api(path, method = "GET", body) {
  const headers = { "Content-Type": "application/json" };
  if (getToken()) headers.Authorization = "Bearer " + getToken();
  const res = await fetch("/api" + path, { method, headers, body: body ? JSON.stringify(body) : undefined });
  if (res.status === 401 && path.startsWith("/tasks")) { logout(); throw new Error("Your session expired. Log in again."); }
  const data = res.status === 204 ? null : await res.json().catch(() => null);
  if (!res.ok) throw new Error(data?.error || "Something went wrong (" + res.status + ").");
  return data;
}

// ---------- View switching (the "SPA" part: one page, two views) ----------
function showView() {
  const loggedIn = !!getToken();
  $("auth-view").hidden = loggedIn;
  $("app-view").hidden = !loggedIn;
  if (loggedIn) { $("who").textContent = localStorage.getItem("email"); loadTasks(); }
}

function setMode(m) {
  mode = m;
  $("tab-login").classList.toggle("active", m === "login");
  $("tab-signup").classList.toggle("active", m === "signup");
  $("auth-submit").textContent = m === "login" ? "Log in" : "Create account";
  $("password").autocomplete = m === "login" ? "current-password" : "new-password";
  $("auth-error").textContent = "";
}

// ---------- Auth ----------
async function submitAuth() {
  try {
    const data = await api("/auth/" + mode, "POST", { email: $("email").value, password: $("password").value });
    localStorage.setItem("token", data.token);
    localStorage.setItem("email", data.email);
    $("password").value = "";
    showView();
  } catch (e) { $("auth-error").textContent = e.message; }
}

function logout() {
  localStorage.removeItem("token");
  localStorage.removeItem("email");
  tasks = []; resetForm(); showView();
}

// ---------- Tasks: Read ----------
async function loadTasks() {
  try { tasks = await api("/tasks"); render(); }
  catch (e) { $("app-error").textContent = e.message; }
}

function render() {
  const list = $("task-list");
  list.innerHTML = "";
  $("empty").hidden = tasks.length > 0;
  for (const t of tasks) {
    const li = document.createElement("li");
    if (t.isDone) li.className = "done";

    const check = document.createElement("input");
    check.type = "checkbox"; check.checked = t.isDone;
    check.setAttribute("aria-label", "Mark done");
    check.onchange = () => save(t.id, { title: t.title, notes: t.notes, isDone: check.checked });

    const body = document.createElement("div");
    body.className = "task-body";
    const title = document.createElement("div");
    title.className = "task-title"; title.textContent = t.title;   // textContent => safe from XSS
    const notes = document.createElement("div");
    notes.className = "task-notes"; notes.textContent = t.notes || "";
    body.append(title, notes);

    const actions = document.createElement("div");
    actions.className = "task-actions";
    const edit = document.createElement("button");
    edit.textContent = "Edit"; edit.onclick = () => startEdit(t);
    const del = document.createElement("button");
    del.textContent = "Delete"; del.className = "danger"; del.onclick = () => remove(t.id);
    actions.append(edit, del);

    li.append(check, body, actions);
    list.append(li);
  }
}

// ---------- Tasks: Create / Update ----------
async function save(id, payload) {
  $("app-error").textContent = "";
  try {
    if (id) await api("/tasks/" + id, "PUT", payload);   // UPDATE
    else await api("/tasks", "POST", payload);           // CREATE
    resetForm(); await loadTasks();
  } catch (e) { $("app-error").textContent = e.message; }
}

function submitForm() {
  const existing = tasks.find((t) => t.id === editingId);
  save(editingId, { title: $("task-title").value, notes: $("task-notes").value, isDone: existing?.isDone ?? false });
}

function startEdit(t) {
  editingId = t.id;
  $("task-title").value = t.title;
  $("task-notes").value = t.notes || "";
  $("task-save").textContent = "Save changes";
  $("task-cancel").hidden = false;
  $("task-title").focus();
}

function resetForm() {
  editingId = null;
  $("task-title").value = ""; $("task-notes").value = "";
  $("task-save").textContent = "Add task";
  $("task-cancel").hidden = true;
}

// ---------- Tasks: Delete ----------
async function remove(id) {
  if (!confirm("Delete this task?")) return;
  try { await api("/tasks/" + id, "DELETE"); await loadTasks(); }
  catch (e) { $("app-error").textContent = e.message; }
}

// ---------- Wire up events ----------
$("tab-login").onclick = () => setMode("login");
$("tab-signup").onclick = () => setMode("signup");
$("auth-submit").onclick = submitAuth;
$("password").onkeydown = (e) => { if (e.key === "Enter") submitAuth(); };
$("logout").onclick = logout;
$("task-save").onclick = submitForm;
$("task-cancel").onclick = resetForm;
$("task-title").onkeydown = (e) => { if (e.key === "Enter") submitForm(); };

showView();
