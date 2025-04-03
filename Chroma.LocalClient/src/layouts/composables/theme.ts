import { defineStore } from 'pinia';

export const useThemeStore = defineStore('theme', () => {
    const dark = useLocalStorage<boolean>('theme-select-dark', true);
    const lightTheme = 'aura-light-amber';
    const darkTheme = 'aura-dark-amber';

    const css_href = computed(() => {
        const theme = dark.value ? darkTheme : lightTheme;

        return `/themes/${theme}/theme.css`;
    });

    const css_link = document.getElementById('theme-css');

    watch(
        css_href,
        () => {
            css_link?.setAttribute('href', css_href.value);
        },
        { immediate: true },
    );

    return {
        dark,
        css_href,
    };
});
