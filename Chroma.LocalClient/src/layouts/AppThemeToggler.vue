<script setup lang="ts">
import { useThemeStore } from "@/layouts/composables/theme.ts";

const themeStore = useThemeStore();

const icon = computed(() => (themeStore.dark ? "pi-moon" : "pi-sun"));

const toggle = () => {
    themeStore.dark = !themeStore.dark;
};
</script>

<template>
    <button class="p-link layout-topbar-button" @click="toggle">
        <i class="pi" :class="icon"></i>
        <span>Change Theme</span>
    </button>
</template>
<style scoped lang="scss"></style>
