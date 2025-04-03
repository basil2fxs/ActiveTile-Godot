import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import Components from "unplugin-vue-components/vite";
import AutoImport from "unplugin-auto-import/vite";
import Pages from "vite-plugin-pages"; // @ts-ignore
import Layouts from "vite-plugin-vue-layouts";

import { PrimeVueResolver } from "unplugin-vue-components/resolvers";
import { VitePWA } from "vite-plugin-pwa";
export default defineConfig({
    server: {
        proxy: {
            "^/api": {
                target: "https://localhost:7246/",
                secure: false,
            },
        },
        port: 5173,
        open: true,
    },
    build: {
        chunkSizeWarningLimit: 1000,
    },
    plugins: [
        vue(),
        Pages(),
        Layouts(),
        AutoImport({
            imports: ["vue", "vue-router", "@vueuse/core"],
            dts: "src/auto-imports.d.ts",
            dirs: ["src/composables", "src/stores"],
            vueTemplate: true,
            eslintrc: {
                enabled: true,
            },
        }),
        Components({
            resolvers: [
                PrimeVueResolver({
                    importStyle: true,
                    importIcons: true,
                }),
            ],
            dts: "src/components.d.ts",
        }),
        VitePWA({
            registerType: "prompt",
            devOptions: {
                enabled: true,
            },
            manifest: {
                name: "Chroma",
                short_name: "Chroma",
                theme_color: "#5f52b3",
                description: "Chroma",
                icons: [
                    {
                        src: "/256x256.png",
                        sizes: "256x256",
                        type: "image/png",
                    },
                ],
            },
        }),
    ],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
        },
    },
    css: {},
});
