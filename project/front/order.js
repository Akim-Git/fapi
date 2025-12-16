// tady si podle prostředí volím backend
// lokálně mířím na localhost, v produkci na Render
const API_BASE =
  (location.protocol === "file:" || location.hostname === "localhost")
    ? "https://localhost:7242"
    : "https://fapi-backend.onrender.com";

// tady si beru reference na formulář a ovládací prvky
const form = document.getElementById("orderForm");
const productPick = document.getElementById("productPick");
const productQty = document.getElementById("productQty");
const addToCartBtn = document.getElementById("addToCartBtn");
const cartBody = document.getElementById("cartBody");

const curInput = document.getElementById("targetCurrency");
const quoteBox = document.getElementById("quoteBox");
const quoteErr = document.getElementById("quoteErr");

// tady si držím katalog produktů z backendu
let catalog = [];

// košík si držím jako Mapu (productId -> quantity)
const cart = new Map();

// tady mažu staré chyby a případně vypíšu nové
function setErrors(errors) {
  document.querySelectorAll("[data-err]").forEach(el => el.textContent = "");
  if (!errors) return;

  for (const [k, v] of Object.entries(errors)) {
    const el = document.querySelector(`[data-err="${k}"]`);
    if (el) el.textContent = v;
  }
}

// tady načítám produkty z backendu
async function loadProducts() {
  const r = await fetch(`${API_BASE}/api/products`);
  catalog = await r.json();

  // smažu staré optiony
  productPick.innerHTML = "";

  // tady vytvářím optiony bezpečně přes DOM (nepoužívám innerHTML)
  for (const p of catalog) {
    const opt = document.createElement("option");
    opt.value = String(p.id);
    opt.textContent = `${p.name} – ${p.price} CZK`; // textContent = automaticky safe
    productPick.appendChild(opt);
  }
}

// tady si převádím Map košíku na strukturu pro API
function cartItemsForApi() {
  return Array.from(cart.entries()).map(([productId, quantity]) => ({
    productId: Number(productId),
    quantity: Number(quantity)
  }));
}

// tady vykresluju košík do tabulky
function renderCart() {
  const items = cartItemsForApi();

  // pokud je košík prázdný, jen to oznámím
  if (items.length === 0) {
    // tady klidně použiju jednoduché innerHTML s konstantním textem (bez dat od uživatele)
    cartBody.innerHTML = `<tr><td colspan="5">Košík je prázdný.</td></tr>`;
    return;
  }

  // tady si vytvořím mapu produktů pro rychlé hledání
  const pMap = new Map(catalog.map(p => [p.id, p]));

  // tady kompletně vyčistím tbody a znovu ho poskládám přes DOM API
  cartBody.innerHTML = "";

  for (const it of items) {
    const p = pMap.get(it.productId);
    const unit = p ? Number(p.price) : 0;
    const line = unit * it.quantity;

    const tr = document.createElement("tr");
    tr.dataset.productId = String(it.productId);

    // 1) název produktu
    const tdName = document.createElement("td");
    tdName.textContent = p?.name ?? `ProductId: ${it.productId}`;

    // 2) jednotková cena
    const tdUnit = document.createElement("td");
    tdUnit.className = "right";
    tdUnit.textContent = String(unit);

    // 3) množství (input)
    const tdQty = document.createElement("td");
    tdQty.className = "right";

    const input = document.createElement("input");
    input.type = "number";
    input.min = "1";
    input.max = "999";
    input.value = String(it.quantity);
    input.className = "lineQty";

    // tady reaguju na změnu množství v řádku
    input.addEventListener("input", () => {
      const pid = Number(tr.dataset.productId);
      const q = Number(input.value);

      // potvrzuju, že množství je v povoleném rozsahu
      if (Number.isInteger(q) && q >= 1 && q <= 999) {
        cart.set(pid, q);
        renderCart();
        refreshQuote();
      }
    });

    tdQty.appendChild(input);

    // 4) mezisoučet řádku
    const tdLine = document.createElement("td");
    tdLine.className = "right";

    const b = document.createElement("b");
    b.textContent = String(line);
    tdLine.appendChild(b);

    // 5) odebrání položky
    const tdRemove = document.createElement("td");
    tdRemove.className = "right";

    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "removeBtn";
    btn.textContent = "Odebrat";

    // tady řeším odebrání položky z košíku
    btn.addEventListener("click", () => {
      const pid = Number(tr.dataset.productId);
      cart.delete(pid);
      renderCart();
      refreshQuote();
    });

    tdRemove.appendChild(btn);

    // tady poskládám řádek do tabulky
    tr.appendChild(tdName);
    tr.appendChild(tdUnit);
    tr.appendChild(tdQty);
    tr.appendChild(tdLine);
    tr.appendChild(tdRemove);

    cartBody.appendChild(tr);
  }
}

// tady přidávám položku do košíku
addToCartBtn.addEventListener("click", () => {
  setErrors(null);

  const pid = Number(productPick.value);
  const qty = Number(productQty.value);

  // potvrzuju, že množství je validní
  if (!Number.isInteger(qty) || qty < 1 || qty > 999) {
    setErrors({ items: "Množství musí být 1–999." });
    return;
  }

  // pokud už produkt v košíku je, jen ho navýším
  const prev = cart.get(pid) || 0;
  cart.set(pid, prev + qty);

  renderCart();
  refreshQuote();
});

// tady si nechávám spočítat cenu z backendu (quote)
async function refreshQuote() {
  quoteErr.textContent = "";

  const items = cartItemsForApi();
  const targetCurrency = String(curInput.value || "CZK").toUpperCase();

  if (items.length === 0) {
    quoteBox.textContent = "Přidej položky do košíku…";
    return;
  }

  quoteBox.textContent = "Načítám…";

  // posílám košík do POST /api/orders/quote
  const r = await fetch(`${API_BASE}/api/orders/quote`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ items, targetCurrency })
  });

  const data = await r.json();

  if (!r.ok) {
    quoteBox.textContent = "Nelze spočítat.";
    quoteErr.textContent = data.error || "Chyba.";
    return;
  }

  // tady vypisuju rekapitulaci z backendu
  quoteBox.textContent =
    `Mezisoučet: ${data.subtotalCzk} CZK\n` +
    `DPH: ${data.vatCzk} CZK\n` +
    `Celkem: ${data.totalCzk} CZK\n\n` +
    `Kurz: 1 ${data.targetCurrency} = ${data.rateCzkPerUnit} CZK\n` +
    `Celkem: ${data.totalInCurrency} ${data.targetCurrency}`;
}

// tady odesílám celou objednávku
form.addEventListener("submit", async (e) => {
  e.preventDefault();
  setErrors(null);

  const items = cartItemsForApi();
  if (items.length === 0) {
    setErrors({ items: "Košík je prázdný." });
    return;
  }

  // tady připravuju payload zákazníka
  const customerPayload = {
    id: 0,
    name: form.fullName.value,
    email: form.email.value,
    phone: form.phone.value,
    address: form.address.value
  };

  // 1) nejdřív vytvořím zákazníka
  const cRes = await fetch(`${API_BASE}/api/customers`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(customerPayload)
  });

  const cData = await cRes.json();
  if (!cRes.ok) {
    setErrors(cData.errors || { general: cData.error });
    return;
  }

  // 2) potom vytvořím objednávku
  const orderPayload = {
    id: 0,
    customerId: cData.id,
    items,
    targetCurrency: String(curInput.value || "CZK").toUpperCase(),
    total: 0
  };

  const oRes = await fetch(`${API_BASE}/api/orders`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(orderPayload)
  });

  const oData = await oRes.json();
  if (!oRes.ok) {
    setErrors(oData.errors || { general: oData.error });
    return;
  }

  // tady přesměrovávám na děkovací stránku
  location.href = `./thanks.html?id=${encodeURIComponent(oData.orderId)}`;
});

// tady přepočítávám cenu při změně měny
["change", "input"].forEach(evt => {
  curInput.addEventListener(evt, refreshQuote);
});

// inicializace stránky
(async function init() {
  await loadProducts();
  renderCart();
})();
