import { promises as fs } from "node:fs";
import path from "node:path";
import { gunzip } from "node:zlib";
import { promisify } from "node:util";

const gunzipAsync = promisify(gunzip);
const workspace = process.cwd();
const source = path.resolve(process.argv[2] ?? "WebGLBuild");
const destination = path.resolve(process.argv[3] ?? "_site");

if (source === destination) throw new Error("El origen y el destino no pueden ser iguales.");
if (destination === workspace || !destination.startsWith(`${workspace}${path.sep}`)) {
  throw new Error("El destino debe ser una carpeta específica dentro del proyecto.");
}

const sourceIndex = path.join(source, "index.html");
await fs.access(sourceIndex);

await fs.rm(destination, { recursive: true, force: true });
await fs.cp(source, destination, {
  recursive: true,
  filter: (entry) => !entry.includes("UnityProject_BurstDebugInformation_DoNotShip"),
});

async function expandGzipFiles(directory) {
  const entries = await fs.readdir(directory, { withFileTypes: true });
  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      await expandGzipFiles(fullPath);
      continue;
    }
    if (!entry.isFile() || !entry.name.endsWith(".gz")) continue;

    const outputPath = fullPath.slice(0, -3);
    const compressed = await fs.readFile(fullPath);
    await fs.writeFile(outputPath, await gunzipAsync(compressed));
    // Conservar la copia .gz evita que proveedores con archivos bajo demanda
    // propaguen por error una eliminación al origen. index.html usa la versión
    // expandida, por lo que Pages nunca intenta servir este archivo.
    console.log(`Descomprimido: ${path.relative(destination, outputPath)}`);
  }
}

await expandGzipFiles(destination);

const indexPath = path.join(destination, "index.html");
const originalIndex = await fs.readFile(indexPath, "utf8");
const deployIndex = originalIndex
  .replaceAll("WebGLBuild.data.gz", "WebGLBuild.data")
  .replaceAll("WebGLBuild.framework.js.gz", "WebGLBuild.framework.js")
  .replaceAll("WebGLBuild.wasm.gz", "WebGLBuild.wasm");
await fs.writeFile(indexPath, deployIndex);
await fs.writeFile(path.join(destination, ".nojekyll"), "");

const required = [
  "index.html",
  "Build/WebGLBuild.loader.js",
  "Build/WebGLBuild.data",
  "Build/WebGLBuild.framework.js",
  "Build/WebGLBuild.wasm",
  "StreamingAssets/DigestiveSimulator/catalogs/species_catalog.json",
];
for (const relativePath of required) {
  await fs.access(path.join(destination, relativePath));
}
if (deployIndex.includes(".gz")) {
  throw new Error("index.html todavía contiene referencias .gz.");
}

console.log(`Artefacto GitHub Pages preparado en ${destination}`);
