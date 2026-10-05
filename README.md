# CDMAuditorias

Web de **auditorías de calidad del CDM**: el informe de Power BI «CDM Auditorías Calidad»
pasado a ASP.NET Core MVC (.NET 8, C#, Razor), con sus mismas medidas y filtros.

- **General** y **Formación & Calidad**: 5 indicadores (total, semana, mes, nota promedio y total
  de agentes en nómina frente a auditados), evolución de auditorías y de la nota por día, semana o
  mes, auditorías y nota por sector, y top 10 de auditores.
- Filtros en cascada (fecha, mes, sector, super, team, auditor, cargo, base) que se aplican al
  marcar, clic en un sector o un auditor para filtrar, y descarga del detalle en Excel.
- Datos de SQL Server (la misma consulta del Power BI, en `CDM Auditorias Calidad/Consultas`),
  en memoria y recargados cada 30 minutos.
- **CDM No solución** (`/nosolucion`, traído de ranking-mvc): la encuesta de solución de las
  llamadas de Call Bogotá (YOIGO, MASMOVIL, JAZZTEL y ORANGE), con resumen, equipos, motivos y
  sin acceso a internet, y exportación a CSV. Sale de BigQuery por ODBC (DSN `BQCOL`), en un cubo
  de 90 días guardado en disco que se renueva cada 12 horas.
- Aspecto según `docs/guia-de-estilos.md` (sección 9: lo propio de esta web), con tema claro y oscuro.

## Ponerlo en marcha

1. Copiar `CDM Auditorias Calidad/env.ejemplo` como `CDM Auditorias Calidad/.env` y poner las
   credenciales de SQL Server (el `.env` no se sube al repositorio).
2. Abrir `CDM Auditorias Calidad.sln` en Visual Studio y pulsar F5, o ejecutar
   `dotnet run --launch-profile http` en `CDM Auditorias Calidad/` → `http://localhost:5180/general`.
3. Pruebas: `dotnet test "CDM Auditorias Calidad.sln"`.
4. Para servirla a los compañeros: `publicar.cmd` y luego `arrancar.cmd`, en el mismo puerto
   5180 (`http://<IP del equipo>:5180/general`). Solo puede haber una en marcha a la vez.

## Documentación

**`CONTEXTO_IA.md`** lo cuenta todo: el análisis del Power BI, cómo se pasó cada medida DAX,
las decisiones, la estructura, cómo se ejecuta y publica, lo comprobado y lo pendiente.
