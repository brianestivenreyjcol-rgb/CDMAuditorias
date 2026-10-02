// CDM Auditorías Calidad — comportamiento de las páginas.
//
// Sin JavaScript todo funciona con enlaces y un formulario GET. Con él:
// - Filtros: se aplican al cerrar el desplegable (o con «Aplicar») y al cambiar una fecha. Los
//   enlaces con data-parcial (Día/Semana/Mes, clic en un sector o un auditor, borrar filtros)
//   no recargan: se pide la página con la cabecera X-Parcial y se sustituye #informe.
// - Fichas: cada <title> de las gráficas («Etiqueta · Serie valor · …») y el title de las
//   piezas marcadas se convierte en una ficha al pasar el ratón, con guía vertical.
// - Tablas table.ranking[data-mapa]: se ordenan al pulsar la cabecera y se colorean con el
//   semáforo según th[data-sentido] (1 más es mejor, -1 más es peor, 0 sin color).
// - Cifras de la tira de indicadores: cuentan desde cero al aparecer.
// Con «reducir movimiento» no se anima nada.
(() => {
  'use strict';

  const sinMovimiento = window.matchMedia('(prefers-reduced-motion: reduce)');
  const raiz = document.documentElement;

  // ---------------------------------------------------------------------------------------
  // Carga parcial
  // ---------------------------------------------------------------------------------------

  let peticion = null;

  async function cargar(url, empujar = true) {
    const informe = document.getElementById('informe');
    if (!informe) { location.href = url; return; }

    peticion?.abort();
    peticion = new AbortController();
    raiz.classList.add('cargando');
    const masFiltrosAbierto = informe.querySelector('.mas-filtros')?.open;

    try {
      const r = await fetch(url, { headers: { 'X-Parcial': '1' }, signal: peticion.signal });
      if (!r.ok) throw new Error('HTTP ' + r.status);
      const molde = document.createElement('template');
      molde.innerHTML = await r.text();
      const nuevo = molde.content.querySelector('#informe');
      if (!nuevo) throw new Error('Respuesta sin informe');

      const mas = nuevo.querySelector('.mas-filtros');
      if (mas && masFiltrosAbierto) mas.open = true;

      ocultarFicha();
      informe.replaceWith(nuevo);
      preparar(nuevo);
      if (nuevo.dataset.titulo) document.title = nuevo.dataset.titulo;
      if (empujar) history.pushState({ informe: true }, '', url);
    } catch (e) {
      if (e.name === 'AbortError') return;
      location.href = url; // Si falla, recarga normal.
      return;
    }
    raiz.classList.remove('cargando');
  }

  function urlDe(form) {
    // Las fechas que coinciden con el borde del calendario no se envían: así la URL sigue
    // sirviendo cuando lleguen datos nuevos (el rango se amplía solo, como en el PBI).
    const bordes = {
      desde: form.querySelector('input[name=desde]')?.min,
      hasta: form.querySelector('input[name=hasta]')?.max,
    };
    const p = new URLSearchParams();
    for (const [clave, valor] of new FormData(form)) {
      if (valor === '' || (clave in bordes && valor === bordes[clave])) continue;
      p.append(clave, valor);
    }
    const q = p.toString();
    return form.getAttribute('action') + (q ? '?' + q : '');
  }

  const marcadas = d => [...d.querySelectorAll('input[type=checkbox]:checked')].map(c => c.value).join('\u0001');

  function enviar(form) {
    // Que el cierre de los desplegables no vuelva a enviar.
    form.querySelectorAll('details.multi[open]').forEach(d => { d.dataset.inicial = marcadas(d); d.open = false; });
    cargar(urlDe(form));
  }

  const sinTildes = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();

  document.addEventListener('click', e => {
    const enlace = e.target.closest('a[data-parcial]');
    if (enlace && e.button === 0 && !e.ctrlKey && !e.metaKey && !e.shiftKey && !e.altKey) {
      e.preventDefault();
      cargar(enlace.href);
      return;
    }

    const limpiar = e.target.closest('[data-limpiar]');
    if (limpiar) {
      const d = limpiar.closest('details');
      d.querySelectorAll('input[type=checkbox]').forEach(c => { c.checked = false; });
      enviar(d.closest('form'));
      return;
    }

    const ordenar = e.target.closest('table.ranking .ordenar');
    if (ordenar) {
      ordenarTabla(ordenar.closest('th'));
      return;
    }

    // Clic fuera: cierra los desplegables abiertos (y aplica si cambió algo).
    document.querySelectorAll('details.multi[open]').forEach(d => { if (!d.contains(e.target)) d.open = false; });
  });

  // «toggle» no burbujea: se escucha en fase de captura.
  document.addEventListener('toggle', e => {
    const d = e.target;
    if (!(d instanceof HTMLDetailsElement) || !d.matches('details.multi')) return;
    if (d.open) {
      document.querySelectorAll('details.multi[open]').forEach(o => { if (o !== d) o.open = false; });
      d.dataset.inicial = marcadas(d);
      d.querySelector('.buscar-opcion')?.focus();
    } else if (d.dataset.inicial !== undefined && d.dataset.inicial !== marcadas(d) && d.isConnected) {
      enviar(d.closest('form'));
    }
  }, true);

  document.addEventListener('submit', e => {
    const form = e.target;
    if (!form.matches('[data-tablero-filtros]')) return;
    e.preventDefault();
    enviar(form);
  });

  document.addEventListener('change', e => {
    const campo = e.target;
    if (campo.matches('[data-tablero-filtros] input[type=date]') && campo.value) enviar(campo.form);
  });

  document.addEventListener('input', e => {
    const buscar = e.target;
    if (!buscar.matches('.buscar-opcion')) return;
    const texto = sinTildes(buscar.value);
    buscar.closest('.desplegable').querySelectorAll('label.opcion').forEach(l => {
      l.hidden = texto !== '' && !sinTildes(l.textContent).includes(texto);
    });
  });

  document.addEventListener('keydown', e => {
    if (e.key !== 'Escape') return;
    ocultarFicha();
    document.querySelectorAll('details.multi[open]').forEach(d => {
      d.open = false;
      d.querySelector('summary')?.focus();
    });
  });

  window.addEventListener('popstate', () => {
    if (document.getElementById('informe')) cargar(location.href, false);
  });

  // ---------------------------------------------------------------------------------------
  // Fichas al pasar el ratón
  // ---------------------------------------------------------------------------------------

  const ficha = document.createElement('div');
  ficha.className = 'ficha';
  ficha.setAttribute('role', 'tooltip');
  document.body.appendChild(ficha);
  let objetivo = null;

  // «Etiqueta · Serie valor · …»: la primera parte es el título y cada una de las demás, una
  // fila «Serie | valor» (el valor empieza en la primera cifra o en «—»).
  function pintarFicha(texto) {
    const [titulo, ...resto] = texto.split(' · ');
    ficha.replaceChildren();
    const t = document.createElement('div');
    t.className = 'ficha-titulo';
    t.textContent = titulo;
    ficha.appendChild(t);
    for (const parte of resto) {
      const fila = document.createElement('div');
      fila.className = 'ficha-fila';
      const m = parte.match(/^(.*?)\s+([-+−]?[\d—].*)$/);
      const nombre = document.createElement('span');
      nombre.textContent = m ? m[1] : parte;
      fila.appendChild(nombre);
      if (m) {
        const valor = document.createElement('b');
        valor.textContent = m[2];
        fila.appendChild(valor);
      }
      ficha.appendChild(fila);
    }
  }

  function colocarFicha(x, y) {
    const margen = 14;
    const r = ficha.getBoundingClientRect();
    let izq = x + margen;
    let arriba = y + margen;
    if (izq + r.width > window.innerWidth - 8) izq = x - r.width - margen;
    if (arriba + r.height > window.innerHeight - 8) arriba = y - r.height - margen;
    ficha.style.left = Math.max(8, izq) + 'px';
    ficha.style.top = Math.max(8, arriba) + 'px';
  }

  function ocultarFicha() {
    ficha.classList.remove('visible');
    if (objetivo) quitarFoco(objetivo);
    objetivo = null;
  }

  // Guía vertical y desvanecido del resto de periodos en las gráficas por tiempo.
  function ponerFoco(el) {
    const plano = el.closest('.plano');
    if (!plano || el.dataset.i === undefined) return;
    plano.classList.add('con-foco');
    plano.querySelectorAll(`[data-i="${el.dataset.i}"]`).forEach(x => x.classList.add('foco'));
    let guia = plano.querySelector(':scope > .guia');
    if (!guia) {
      guia = document.createElement('span');
      guia.className = 'guia';
      plano.appendChild(guia);
    }
    guia.style.left = (parseFloat(el.dataset.x) * 100) + '%';
    guia.hidden = false;
  }

  function quitarFoco(el) {
    const plano = el.closest('.plano');
    if (!plano) return;
    plano.classList.remove('con-foco');
    plano.querySelectorAll('.foco').forEach(x => x.classList.remove('foco'));
    const guia = plano.querySelector(':scope > .guia');
    if (guia) guia.hidden = true;
  }

  document.addEventListener('pointermove', e => {
    const el = e.target.closest?.('[data-ficha]');
    if (el !== objetivo) {
      if (objetivo) quitarFoco(objetivo);
      objetivo = el;
      if (!el) { ficha.classList.remove('visible'); return; }
      pintarFicha(el.dataset.ficha);
      ponerFoco(el);
      ficha.classList.add('visible');
    }
    if (el) colocarFicha(e.clientX, e.clientY);
  });
  document.addEventListener('pointerleave', ocultarFicha);
  window.addEventListener('scroll', ocultarFicha, { passive: true });

  // Pasa los <title> de SVG y los title de las piezas a data-ficha (así no sale además el
  // globo del navegador).
  function prepararFichas(contenedor) {
    contenedor.querySelectorAll('svg title').forEach(t => {
      t.parentElement.dataset.ficha = t.textContent.trim();
      t.remove();
    });
    contenedor.querySelectorAll('.resumen-dato[title], .hbarra[title]').forEach(el => {
      el.dataset.ficha = el.title;
      el.removeAttribute('title');
    });
  }

  // ---------------------------------------------------------------------------------------
  // Tablas que se ordenan y colorean
  // ---------------------------------------------------------------------------------------

  const comparar = new Intl.Collator('es', { sensitivity: 'base', numeric: true }).compare;

  function valorDe(celda) {
    const v = celda?.dataset.valor ?? celda?.textContent ?? '';
    const n = v.trim() === '' ? NaN : Number(v);
    return Number.isNaN(n) ? v : n;
  }

  function ordenarTabla(th) {
    const tabla = th.closest('table');
    const columna = [...th.parentElement.children].indexOf(th);
    const ascendente = th.getAttribute('aria-sort') === 'descending';
    tabla.querySelectorAll('th[aria-sort]').forEach(x => x.removeAttribute('aria-sort'));
    th.setAttribute('aria-sort', ascendente ? 'ascending' : 'descending');

    const cuerpo = tabla.tBodies[0];
    const filas = [...cuerpo.rows];
    filas.sort((a, b) => {
      // Las filas con pocos datos y la de totales se quedan siempre al final.
      const ra = a.classList.contains('total') ? 2 : a.classList.contains('pocas') ? 1 : 0;
      const rb = b.classList.contains('total') ? 2 : b.classList.contains('pocas') ? 1 : 0;
      if (ra !== rb) return ra - rb;
      const va = valorDe(a.cells[columna]);
      const vb = valorDe(b.cells[columna]);
      const vacioA = va === '' || (typeof va === 'number' && Number.isNaN(va));
      const vacioB = vb === '' || (typeof vb === 'number' && Number.isNaN(vb));
      if (vacioA !== vacioB) return vacioA ? 1 : -1;
      const r = typeof va === 'number' && typeof vb === 'number' ? va - vb : comparar(String(va), String(vb));
      return ascendente ? r : -r;
    });
    cuerpo.append(...filas);
  }

  function colorearTabla(tabla) {
    const cabeceras = [...tabla.tHead.rows[0].cells];
    cabeceras.forEach((th, columna) => {
      const sentido = Number(th.dataset.sentido || 0);
      if (!sentido) return;
      const celdas = [...tabla.tBodies[0].rows]
        .filter(f => !f.classList.contains('pocas') && !f.classList.contains('total'))
        .map(f => f.cells[columna])
        .filter(c => c && c.dataset.valor !== '' && !Number.isNaN(Number(c.dataset.valor)));
      if (celdas.length < 2) return;
      const valores = celdas.map(c => Number(c.dataset.valor));
      const min = Math.min(...valores);
      const max = Math.max(...valores);
      if (max === min) return;
      celdas.forEach(c => {
        let t = (Number(c.dataset.valor) - min) / (max - min);
        if (sentido < 0) t = 1 - t;
        c.classList.add(t >= 2 / 3 ? 'mapa-verde' : t >= 1 / 3 ? 'mapa-ambar' : 'mapa-rojo');
      });
    });
  }

  // ---------------------------------------------------------------------------------------
  // Cifras que cuentan
  // ---------------------------------------------------------------------------------------

  const entero = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 0, useGrouping: 'always' });
  const porcentaje = new Intl.NumberFormat('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2, useGrouping: 'always' });
  const suavizar = t => 1 - Math.pow(1 - t, 3);

  function contarCifras(contenedor) {
    if (sinMovimiento.matches) return;
    contenedor.querySelectorAll('.resumen-cifra[data-contar]').forEach(el => {
      const final = Number(el.dataset.contar);
      if (el.dataset.contar === '' || Number.isNaN(final)) return;
      const textoFinal = el.textContent;
      const esPorcentaje = el.dataset.formato === 'porcentaje';
      const inicio = performance.now();
      const duracion = 780;
      function paso(ahora) {
        if (!el.isConnected) return;
        const t = Math.min(1, (ahora - inicio) / duracion);
        const v = final * suavizar(t);
        el.textContent = t >= 1 ? textoFinal : esPorcentaje ? porcentaje.format(v * 100) + ' %' : entero.format(v);
        if (t < 1) requestAnimationFrame(paso);
      }
      requestAnimationFrame(paso);
    });
  }

  // ---------------------------------------------------------------------------------------

  function preparar(contenedor) {
    prepararFichas(contenedor);
    contenedor.querySelectorAll('table.ranking[data-mapa]').forEach(colorearTabla);
    contarCifras(contenedor);
  }

  preparar(document);
})();
