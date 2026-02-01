import { ref, computed } from 'vue'
import api from '@/api'
import type { Tag } from '@/types'

export function useTags() {
    const tags = ref<Tag[]>([])

    const fetchTags = async () => {
        try {
            const response = await api.get<Tag[]>('/tags')
            tags.value = response.data
        } catch (error) {
            console.log('Failed to fetch tags.')
            console.error(error)
        }
    }

    return {
        tags,
        fetchTags,
    }
}
