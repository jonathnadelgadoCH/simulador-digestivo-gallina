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

const forbiddenSourceExtensions = new Set([
  ".asmdef",
  ".blend",
  ".cs",
  ".meta",
  ".pdb",
  ".py",
  ".pyc",
]);

async function collectForbiddenFiles(directory, root = directory) {
  const forbidden = [];
  const entries = await fs.readdir(directory, { withFileTypes: true });
  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      if (entry.name.includes("BurstDebugInformation_DoNotShip")) {
        if (root !== source) forbidden.push(path.relative(root, fullPath));
        continue;
      }
      forbidden.push(...await collectForbiddenFiles(fullPath, root));
      continue;
    }

    const lowerName = entry.name.toLowerCase();
    if (
      forbiddenSourceExtensions.has(path.extname(lowerName))
      || lowerName.endsWith(".symbols.json")
      || lowerName.endsWith(".map")
    ) {
      forbidden.push(path.relative(root, fullPath));
    }
  }
  return forbidden;
}

const forbiddenAtSource = await collectForbiddenFiles(source);
if (forbiddenAtSource.length > 0) {
  throw new Error(
    `El build contiene fuentes, símbolos o mapas que no deben publicarse:\n${forbiddenAtSource.join("\n")}`,
  );
}

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
    const compressedExtension = entry.name.endsWith(".unityweb")
      ? ".unityweb"
      : entry.name.endsWith(".gz")
        ? ".gz"
        : null;
    if (!entry.isFile() || compressedExtension === null) continue;

    const outputPath = fullPath.slice(0, -compressedExtension.length);
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
  .replaceAll("WebGLBuild.data.unityweb", "WebGLBuild.data")
  .replaceAll("WebGLBuild.framework.js.unityweb", "WebGLBuild.framework.js")
  .replaceAll("WebGLBuild.wasm.unityweb", "WebGLBuild.wasm")
  .replaceAll("WebGLBuild.data.gz", "WebGLBuild.data")
  .replaceAll("WebGLBuild.framework.js.gz", "WebGLBuild.framework.js")
  .replaceAll("WebGLBuild.wasm.gz", "WebGLBuild.wasm")
  .replace(
    /<head>/i,
    `<head>
    <meta name="referrer" content="no-referrer">
    <meta http-equiv="Content-Security-Policy" content="default-src 'self'; base-uri 'self'; object-src 'none'; form-action 'none'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval' blob:; worker-src 'self' blob:; connect-src 'self' blob: data:; img-src 'self' data: blob:; media-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; font-src 'self' data:">`,
  );
await fs.writeFile(indexPath, deployIndex);
await fs.writeFile(path.join(destination, ".nojekyll"), "");
await fs.writeFile(
  path.join(destination, "_headers"),
  `/*
  Content-Security-Policy: default-src 'self'; base-uri 'self'; object-src 'none'; form-action 'none'; script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval' blob:; worker-src 'self' blob:; connect-src 'self' blob: data:; img-src 'self' data: blob:; media-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; font-src 'self' data:
  Referrer-Policy: no-referrer
  X-Content-Type-Options: nosniff
  Permissions-Policy: camera=(), geolocation=(), microphone=(), payment=(), usb=()
`,
);

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
if (deployIndex.includes(".gz") || deployIndex.includes(".unityweb")) {
  throw new Error("index.html todavía contiene referencias a contenido comprimido.");
}

const forbiddenAtDestination = await collectForbiddenFiles(destination);
if (forbiddenAtDestination.length > 0) {
  throw new Error(
    `El artefacto preparado contiene archivos no publicables:\n${forbiddenAtDestination.join("\n")}`,
  );
}

console.log(`Artefacto GitHub Pages preparado en ${destination}`);
