import { computed, ref, type Ref } from 'vue'

type OptionsSource = Ref<string[]>
type SelectedSource = Ref<string[]>

const normalizeText = (value: string) => value.trim().toLocaleLowerCase('zh-CN')

export const useMultiSelectActions = (options: OptionsSource, selected: SelectedSource) => {
  const keyword = ref('')

  const uniqueSortedOptions = computed(() => {
    return options.value
      .filter((item, index, list) => !!item && list.indexOf(item) === index)
      .sort((a, b) => a.localeCompare(b, 'zh-CN'))
  })

  const filteredOptions = computed(() => {
    const key = normalizeText(keyword.value)
    if (!key) {
      return uniqueSortedOptions.value
    }

    return uniqueSortedOptions.value.filter((item) => normalizeText(item).includes(key))
  })

  const selectAll = () => {
    selected.value = [...uniqueSortedOptions.value]
  }

  const selectFiltered = () => {
    const merged = new Set<string>(selected.value)
    filteredOptions.value.forEach((item) => merged.add(item))
    selected.value = [...merged].sort((a, b) => a.localeCompare(b, 'zh-CN'))
  }

  const clearAll = () => {
    selected.value = []
  }

  const toggleSelection = () => {
    const current = new Set(selected.value)
    selected.value = uniqueSortedOptions.value.filter((item) => !current.has(item))
  }

  return {
    keyword,
    filteredOptions,
    uniqueSortedOptions,
    selectAll,
    selectFiltered,
    clearAll,
    toggleSelection,
  }
}
