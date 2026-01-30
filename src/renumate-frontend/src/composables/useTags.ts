import { ref, computed } from 'vue'
import api from '@/api'
import type { Tag } from '@/types'
import { toast } from 'vue3-toastify'

export function useTags() {
    const tags = ref<Tag[]>([])

    const fetchTags = async () => {
        try {
            const response = await api.get<Tag[]>('/tags')
            tags.value = response.data
        } catch (error) {
            toast.error('Failed to fetch tags.')
            console.error(error)
        }
    }

    return {
        tags,
        fetchTags,
    }
}
