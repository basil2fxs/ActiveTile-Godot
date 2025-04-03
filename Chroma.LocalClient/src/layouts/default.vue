<script setup lang="ts">
import { computed, watch, ref } from "vue";
import AppTopbar from "@/layouts/AppTopbar.vue";
import AppFooter from "@/layouts/AppFooter.vue";
import { useLayout } from "@/layouts/composables/layout.ts";
import { useThemeStore } from "@/layouts/composables/theme.ts";
import AppSidebar from "@/layouts/AppSidebar.vue";

const { layoutConfig, layoutState, isSidebarActive } = useLayout();
const themeStore = useThemeStore();
const outsideClickListener = ref(null);

watch(isSidebarActive, (newVal) => {
    if (newVal) {
        bindOutsideClickListener();
    } else {
        unbindOutsideClickListener();
    }
});

const containerClass = computed(() => {
    return {
        "layout-theme-light": themeStore.dark === true,
        "layout-theme-dark": themeStore.dark === false,
        "layout-overlay": layoutConfig.menuMode.value === "overlay",
        "layout-static": layoutConfig.menuMode.value === "static",
        "layout-static-inactive":
            layoutState.staticMenuDesktopInactive.value &&
            layoutConfig.menuMode.value === "static",
        "layout-overlay-active": layoutState.overlayMenuActive.value,
        "layout-mobile-active": layoutState.staticMenuMobileActive.value,
        "p-ripple-disabled": layoutConfig.ripple.value === false,
    };
});
const bindOutsideClickListener = () => {
    if (!outsideClickListener.value) {
        outsideClickListener.value = (event) => {
            if (isOutsideClicked(event)) {
                layoutState.overlayMenuActive.value = false;
                layoutState.staticMenuMobileActive.value = false;
                layoutState.menuHoverActive.value = false;
            }
        };
        document.addEventListener("click", outsideClickListener.value);
    }
};
const unbindOutsideClickListener = () => {
    if (outsideClickListener.value) {
        document.removeEventListener("click", outsideClickListener);
        outsideClickListener.value = null;
    }
};
const isOutsideClicked = (event) => {
    const sidebarEl = document.querySelector(".layout-sidebar");
    const topbarEl = document.querySelector(".layout-menu-button");

    return !(
        sidebarEl.isSameNode(event.target) ||
        sidebarEl.contains(event.target) ||
        topbarEl.isSameNode(event.target) ||
        topbarEl.contains(event.target)
    );
};
</script>

<template>
    <div class="layout-wrapper" :class="containerClass">
        <AppTopbar></AppTopbar>
        <div class="layout-sidebar">
            <AppSidebar></AppSidebar>
        </div>
        <div class="layout-main-container">
            <div class="layout-main">
                <RouterView v-slot="{ Component }" :key="$route.path">
                    <div class="flex flex-column w-full h-full">
                        <Component :is="Component" />
                    </div>
                </RouterView>
            </div>
            <AppFooter></AppFooter>
        </div>
        <div class="layout-mask"></div>
    </div>
    <Toast />
</template>

<style lang="scss" scoped></style>
