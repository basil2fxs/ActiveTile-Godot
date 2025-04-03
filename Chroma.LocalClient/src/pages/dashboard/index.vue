<script setup lang="ts">
import DataView from "primevue/dataview";
import { type GameDefinition, useGameStore } from "@/store/gameStore.ts";

const gameStore = useGameStore();
const games = gameStore.games() as GameDefinition[];

type GameProperty = {
    id: string;
    title: string;
    description: string;
    to: string;
};

const gameProperties = games.map((g) => {
    return {
        id: g.id,
        title: g.displayName,
        description: g.description,
        to: `/games/${g.id}`,
    } as GameProperty;
});

const router = useRouter();
const navigate = (route: string) => {
    router.push(route);
};
</script>

<template>
    <div class="flex flex-column gap-2">
        <span class="text-4xl flex justify-content-start">Dashboard</span>
        <hr />
        <DataView
            :value="gameProperties"
            layout="grid"
            data-key="id"
            :pt="{
                content: {
                    style: { background: 'transparent' },
                },
            }"
        >
            <template #grid="slotProps">
                <div
                    v-for="(item, index) in slotProps.items"
                    :key="index"
                    style="background-color: transparent"
                >
                    <Card
                        class="shadow-3 dashboard-container m-2"
                        @click="navigate(item.to)"
                    >
                        <template #title> {{ item.title }}</template>
                        <template #subtitle> {{ item.description }}</template>
                        <template #content>
                            <hr />
                            <div
                                class="align-self-center justify-content-center w-full"
                                style="
                                    height: 200px;
                                    border: 1px solid var(--surface-ground);
                                "
                            >
                                <span class="m-2">
                                    Add an image here or gif
                                </span>
                            </div>
                        </template>
                    </Card>
                </div>
            </template>
        </DataView>
    </div>
</template>

<style scoped lang="scss">
.dashboard-container {
    background: var(--surface-card);
    cursor: pointer;
}
.dashboard-container:hover {
    background: var(--surface-hover);
    cursor: pointer;
}
</style>
