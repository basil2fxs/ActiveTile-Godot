<script setup lang="ts">
import { useLayout } from "@/layouts/composables/layout.ts";
import { type GameDefinition, useGameStore } from "@/store/gameStore.ts";
const { isSidebarActive, onMenuToggle } = useLayout();

type SidebarMenuItem = {
    label: string;
    icon: string;
    to: string;
};

type SidebarMenuGroup = {
    label: string;
    items: SidebarMenuItem[];
};

const gameStore = useGameStore();
const games = gameStore.games() as GameDefinition[];

const mainPages: SidebarMenuGroup[] = [
    {
        label: "Home",
        items: [
            { label: "Dashboard", icon: "pi pi-fw pi-home", to: "/dashboard" },
            {
                label: "Analytics",
                icon: "pi pi-fw pi-chart-bar",
                to: "/analytics",
            },
        ],
    },
    {
        label: "Games",
        items: games.map((g) => {
            return {
                label: g.displayName,
                icon: "pi pi-fw pi-th-large",
                to: `/games/${g.id}`,
            } as SidebarMenuItem;
        }),
    },
];

const route = useRoute();
const checkActiveRoute = (item) => {
    return route.path === item.to;
};
</script>

<template>
    <div class="flex flex-column h-full">
        <ul class="layout-menu list-none p-3 m-0">
            <template v-for="(item, i) in mainPages" :key="item">
                <p
                    class="layout-menuitem-root-text justify-content-start flex w-full font-bold"
                >
                    {{ item.label }}
                </p>
                <Transition v-if="item.items" name="layout-submenu">
                    <ul class="layout-submenu">
                        <router-link
                            v-for="option in item.items"
                            :key="option.label"
                            tabindex="0"
                            :to="option.to"
                            :class="[
                                { 'active-route': checkActiveRoute(option) },
                            ]"
                        >
                            <i
                                :class="option.icon"
                                class="layout-menuitem-icon"
                            ></i>
                            <span class="font-light">{{ option.label }}</span>
                        </router-link>
                    </ul>
                </Transition>
                <hr />
            </template>
        </ul>
    </div>
</template>

<style lang="scss" scoped></style>
