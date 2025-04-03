<script setup lang="ts">
import { defineProps } from "vue";
import { type GameDefinition, useGameStore } from "@/store/gameStore.ts";
const props = defineProps({
    id: {
        type: String,
        required: true,
    },
});

const gameStore = useGameStore();
const games = gameStore.games() as GameDefinition[];

const gameDefinition = games.filter(
    (g) => g.id === props.id,
)[0] as GameDefinition;

onMounted(() => {
    console.log("onmount");
});
</script>

<template>
    <div class="flex flex-column gap-2">
        <span class="text-4xl flex justify-content-start"
            >Game - {{ gameDefinition.displayName }}</span
        >
        <hr />
        <Card>
            <template #content>
                <div
                    class="flex flex-column justify-content-start w-full align-items-start"
                >
                    <span class="font-semibold">Here you will be able to:</span>
                    <span> - monitor the game in real-time</span>
                    <span> - configure game settings on the fly</span>
                    <span>
                        - receive configurable notifications whenever games
                        start, games are nearly completed, and a game has been
                        completed</span
                    >
                    <span>
                        - receive alerts when an unexpected issue has been
                        encountered by the game engine</span
                    >
                    <span> - players has pressed the HELP button</span>
                    <span> - other future considerations</span>
                </div>
            </template>
        </Card>
    </div>
</template>

<style scoped lang="scss"></style>
