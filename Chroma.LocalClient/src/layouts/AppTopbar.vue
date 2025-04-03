<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from "vue";
import { useRouter } from "vue-router";
import Button from "primevue/button";
import Menu from "primevue/menu";
import { useLayout } from "@/layouts/composables/layout.ts";
import { useThemeStore } from "@/layouts/composables/theme.ts";
import AppThemeToggler from "@/layouts/AppThemeToggler.vue";

const { onMenuToggle } = useLayout();
const themeStore = useThemeStore();

const outsideClickListener = ref(null);
const topbarMenuActive = ref(false);
const router = useRouter();

onMounted(() => {
    bindOutsideClickListener();
});

onBeforeUnmount(() => {
    unbindOutsideClickListener();
});

const logoUrl = computed(() => {
    return `/layout/images/${themeStore.dark ? "logo-white" : "logo-dark"}.svg`;
});

const onTopBarMenuButton = () => {
    topbarMenuActive.value = !topbarMenuActive.value;
};
const onSettingsClick = () => {
    topbarMenuActive.value = false;
    // TODO - add a destination
};
const topbarMenuClasses = computed(() => {
    return {
        "layout-topbar-menu-mobile-active": topbarMenuActive.value,
    };
});

const bindOutsideClickListener = () => {
    if (!outsideClickListener.value) {
        outsideClickListener.value = (event) => {
            if (isOutsideClicked(event)) {
                topbarMenuActive.value = false;
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
    if (!topbarMenuActive.value) return;

    const sidebarEl = document.querySelector(".layout-topbar-menu");
    const topbarEl = document.querySelector(".layout-topbar-menu-button");

    return !(
        sidebarEl.isSameNode(event.target) ||
        sidebarEl.contains(event.target) ||
        topbarEl.isSameNode(event.target) ||
        topbarEl.contains(event.target)
    );
};

const profileMenu = ref<InstanceType<typeof Menu>>();
const profileItems = ref([
    {
        label: "Profile",
        items: [
            {
                label: "View Profile",
                icon: "pi pi-user",
            },
        ],
    },
]);
</script>

<template>
    <div class="layout-topbar">
        <router-link to="/" class="layout-topbar-logo">
            <img :src="logoUrl" alt="logo" />
            <span>CHROMA</span>
        </router-link>

        <Button
            class="p-link layout-menu-button layout-topbar-button"
            @click="onMenuToggle()"
            text
        >
            <i class="pi pi-bars"></i>
        </Button>

        <Button
            class="p-link layout-topbar-menu-button layout-topbar-button"
            @click="onTopBarMenuButton()"
            text
        >
            <i class="pi pi-ellipsis-v"></i>
        </Button>

        <div class="layout-topbar-menu" :class="topbarMenuClasses">
            <span class="flex align-items-center"> Welcome, User</span>
            <Button
                @click="onTopBarMenuButton()"
                class="p-link layout-topbar-button"
                text
            >
                <i class="pi pi-calendar"></i>
                <span>Calendar</span>
            </Button>
            <Button
                @click="profileMenu!.toggle($event)"
                class="p-link layout-topbar-button"
                text
            >
                <i class="pi pi-user"></i>
                <span>Profile</span>
                <Menu ref="profileMenu" :model="profileItems" :popup="true" />
            </Button>
            <AppThemeToggler />
            <Button
                @click="onSettingsClick()"
                class="p-link layout-topbar-button"
                text
            >
                <i class="pi pi-cog"></i>
                <span>Settings</span>
            </Button>
            <!--      <GoogleLogin :callback="callback" prompt />-->
        </div>
    </div>
</template>

<style lang="scss" scoped></style>
