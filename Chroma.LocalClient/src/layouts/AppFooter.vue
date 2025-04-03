<script setup lang="ts">
import { computed } from "vue";
import { useThemeStore } from "@/layouts/composables/theme.ts";
const themeStore = useThemeStore();

const logoUrl = computed(() => {
    return `/layout/images/${themeStore.dark ? "logo-white" : "logo-dark"}.svg`;
});
</script>

<template>
    <div class="layout-footer">
        <img :src="logoUrl" alt="Logo" height="20" class="mr-2" />
        <span class="font-medium ml-2">Mystic Clue</span>
    </div>
</template>
<style lang="scss" scoped></style>
