<script setup>
import { onMounted, ref, watch } from 'vue'
import IconSpinner from './icons/IconSpinner.vue'
import DatePicker from 'primevue/datepicker';
import MultiSelect from 'primevue/multiselect';
import { useTags } from '@/composables/useTags';

const { tags, fetchTags } = useTags();

const props = defineProps({
  isOpen: Boolean,
  isEditing: Boolean,
  isSubmitting: Boolean,
  modelValue: Object,
  nextRenewalDate: String,
  formatDateTime: {
    type: Function,
    required: true,
  },
  errors: {
    type: Object,
    default: () => ({}),
  },
})

const currencies = [
  'USD',
  'EUR',
  'UAH',
  'PLN',
  'GBP',
  'JPY',
  'CAD',
  'AUD',
  'CHF',
  'CNY',
  'INR',
  'BRL',
  'TRY',
  'MXN',
  'SGD',
  'HKD',
  'SEK',
  'NOK',
  'ILS',
  'ZAR',
]

defineEmits(['close', 'save'])

const formattedCost = ref('')

watch(
  () => props.isOpen,
  (newVal) => {
    if (newVal) {
      formattedCost.value = props.modelValue?.cost?.toString().replace('.', ',') || ''
    }
  },
  { immediate: true }
)

const handleCostInput = (event) => {
  let value = event.target.value
  
  value = value.replace(/[^0-9,.]/g, '')
  
  const normalizedValue = value.replace(',', '.')
  
  const parts = normalizedValue.split('.')
  if (parts.length > 2) return

  formattedCost.value = value

  const numericValue = parseFloat(normalizedValue)
  props.modelValue.cost = isNaN(numericValue) ? 0 : numericValue
}

const setToday = () => {
  props.modelValue.startDate = new Date()
}

onMounted(() => {
  fetchTags()
})

</script>

<template>
  <Transition name="modal">
    <div
      v-if="isOpen"
      class="fixed inset-0 z-[1000] flex items-end sm:items-center justify-center p-2 sm:p-6"
    >
      <div
        class="fixed inset-0 bg-slate-900/60 backdrop-blur-md transition-opacity"
        @click="$emit('close')"
      ></div>

      <div
        class="relative bg-white rounded-[2.5rem] shadow-2xl max-w-2xl w-full flex flex-col transform transition-all border border-slate-100 max-h-[calc(100dvh-2rem)] sm:max-h-[90vh] overflow-hidden"
      >
        <div class="px-8 pt-8 pb-4 flex justify-between items-center">
          <div>
            <h2 class="text-3xl font-black text-slate-900 tracking-tight">
              {{ isEditing ? 'Edit' : 'New' }}
              <span class="text-indigo-600">Subscription</span>
            </h2>
            <p class="text-slate-500 text-sm mt-1">Fill in the details to track your spending.</p>
          </div>
          <button
            @click="$emit('close')"
            class="p-2 hover:bg-slate-100 rounded-full transition-colors text-slate-400"
          >
            <i class="pi pi-times" style="font-size: 1.5rem"></i>
          </button>
        </div>

        <div class="p-8 space-y-6 max-h-[70vh] overflow-y-auto scrollbar-thin">
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div class="md:col-span-2">
              <label
                class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
                >Service Name</label
              >
              <input
                v-model="modelValue.name"
                type="text"
                class="w-full px-5 py-3.5 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all text-slate-900 placeholder:text-slate-300 shadow-sm"
                placeholder="e.g. Netflix, ChatGPT, Spotify"
              />
              <p v-if="errors.Name" class="mt-2 text-xs text-red-500 flex items-center gap-1">
                <span class="w-1 h-1 bg-red-500 rounded-full inline-block"></span>
                {{ errors.Name[0] }}
              </p>
            </div>

            <div>
              <label
                class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
                >{{ modelValue.plan === 'Trial' ? 'Post-Trial' : '' }} Cost</label
              >
              <div class="relative">
                <input
                  :value="formattedCost"
                  @input="handleCostInput"
                  type="text"
                  inputmode="decimal"
                  placeholder="0,00"
                  class="w-full pl-5 pr-12 py-3.5 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all font-mono font-bold"
                />
                <span class="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 font-bold">
                  {{ modelValue.currency }}
                </span>
              </div>
              <p v-if="errors.Cost" class="mt-2 text-xs text-red-500 flex items-center gap-1">
                <span class="w-1 h-1 bg-red-500 rounded-full inline-block"></span>
                {{ errors.Cost[0] }}
              </p>
            </div>

            <div>
              <label
                class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
                >Currency</label
              >
              <select
                v-model="modelValue.currency"
                class="w-full px-5 py-3.5 bg-slate-50 border-none rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all appearance-none cursor-pointer"
              >
                <option v-for="currency in currencies" :key="currency" :value="currency">
                  {{ currency }}
                </option>
              </select>
            </div>
          </div>

          <div class="bg-indigo-50/50 p-6 rounded-[1.5rem] border border-indigo-100/50">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label
                  class="text-xs font-bold uppercase tracking-wider text-indigo-400 mb-2 block ml-1"
                  >Billing Plan</label
                >
                <select
                  v-model="modelValue.plan"
                  class="w-full px-4 py-3 bg-white border-none rounded-xl focus:ring-2 focus:ring-indigo-500 shadow-sm"
                >
                  <option value="Trial">Free Trial</option>
                  <option value="Monthly">Monthly</option>
                  <option value="Quarterly">Quarterly</option>
                  <option value="Annual">Annual</option>
                  <option value="Custom">Custom</option>
                </select>
              </div>

              <div>
                <label
                  class="text-xs font-bold uppercase tracking-wider text-indigo-400 mb-2 block ml-1"
                  >Start Date</label
                >
                <DatePicker v-model="modelValue.startDate" dateFormat="dd/mm/yy" inputClass="rounded-lg" showIcon></DatePicker>
               
                <p v-if="errors.StartDate" class="mt-2 text-xs text-red-500 flex items-center gap-1">
                  <span class="w-1 h-1 bg-red-500 rounded-full inline-block"></span>
                  {{ errors.StartDate[0] }}
                </p>
                <button @click="setToday" class="mt-2 text-xs text-indigo-500 hover:text-indigo-700">Today</button>
              </div>
              <div>
                <label
                  class="text-xs font-bold uppercase tracking-wider text-indigo-400 mb-2 block ml-1"
                  >Auto-Calculated Renewal</label
                >
                <div
                  class="w-full px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg text-indigo-600"
                >
                  {{ formatDateTime(nextRenewalDate, true) || 'Select a start date' }}
                </div>
              </div>
            </div>

            <div
              v-if="modelValue.plan === 'Trial' || modelValue.plan === 'Custom'"
              class="mt-4 pt-4 border-t border-indigo-100"
            >
              <label
                class="text-xs font-bold uppercase tracking-wider text-indigo-400 mb-2 block ml-1"
              >
                {{ modelValue.plan === 'Trial' ? 'Trial Duration (Days)' : 'Every X Days' }}
              </label>
              <input
                v-model="
                  modelValue[
                    modelValue.plan === 'Trial' ? 'trialPeriodInDays' : 'customPeriodInDays'
                  ]
                "
                type="number"
                class="w-full px-4 py-3 bg-white border-none rounded-xl focus:ring-2 focus:ring-indigo-500 shadow-sm"
              />
              <p
                v-if="errors.TrialPeriodInDays"
                class="mt-2 text-xs text-red-500 flex items-center gap-1"
              >
                <span class="w-1 h-1 bg-red-500 rounded-full inline-block"></span>
                {{ errors.TrialPeriodInDays[0] }}
              </p>
              <p
                v-if="errors.CustomPeriodInDays"
                class="mt-2 text-xs text-red-500 flex items-center gap-1"
              >
                <span class="w-1 h-1 bg-red-500 rounded-full inline-block"></span>
                {{ errors.CustomPeriodInDays[0] }}
              </p>
            </div>
          </div>
          <div>
            <label
              class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
              >Tags</label
            >
            <MultiSelect
              v-model="modelValue.tags"
              :options="tags"
              dataKey="id"
              filter optionLabel="name"
              showClear
              placeholder="Select tags"
              class="w-full custom-multiselect"
              display="chip"
            >
              <template #chip="slotProps">
                <div 
                  class="flex items-center gap-2 px-2 py-0.5 rounded-lg border mr-1 transition-all hover:pr-1 group"
                  :style="{ 
                    backgroundColor: slotProps.value.color + '15', 
                    borderColor: slotProps.value.color + '40' 
                  }"
                >
                  <div 
                    class="w-1.5 h-1.5 rounded-full" 
                    :style="{ backgroundColor: slotProps.value.color }"
                  ></div>
                  
                  <span class="text-[11px] font-bold" :style="{ color: slotProps.value.color }">
                    {{ slotProps.value.name }}
                  </span>

                  <button 
                    type="button"
                    @click.stop="slotProps.removeCallback($event)"
                    class="flex items-center justify-center w-4 h-4 rounded-md hover:bg-black/5 transition-colors cursor-pointer"
                  >
                    <svg 
                      xmlns="http://www.w3.org/2000/svg" 
                      class="h-3 w-3" 
                      fill="none" 
                      viewBox="0 0 24 24" 
                      stroke="currentColor"
                      :style="{ stroke: slotProps.value.color }"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </template>
            </MultiSelect>
          </div>
          <div class="space-y-4">
            <div class="group">
              <label
                class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
                >Logo URL</label
              >
              <input
                v-model="modelValue.picLink"
                type="url"
                class="w-full px-5 py-3 bg-white border border-slate-200 rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all outline-none"
                placeholder="https://..."
              />
            </div>
            <div class="group">
              <label
                class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
                >Cancel link</label
              >
              <input
                v-model="modelValue.cancelLink"
                type="url"
                class="w-full px-5 py-3 bg-white border border-slate-200 rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all outline-none"
                placeholder="https://..."
              />
            </div>

            <div class="group">
              <label
                class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
                >Note</label
              >
              <textarea
                v-model="modelValue.note"
                rows="2"
                class="w-full px-5 py-3 bg-white border border-slate-200 rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all outline-none resize-none"
              ></textarea>
            </div>
          </div>
        </div>

        <div class="p-8 bg-slate-50/80 backdrop-blur-sm flex gap-4">
          <button
            @click="$emit('save')"
            :disabled="isSubmitting"
            class="flex-[2] bg-indigo-600 text-white py-4 rounded-2xl hover:bg-indigo-700 active:scale-[0.98] transition-all font-bold shadow-lg shadow-indigo-200 disabled:opacity-50 flex items-center justify-center gap-2 cursor-pointer"
          >
            <IconSpinner v-if="isSubmitting" class="h-5 w-5" />
            {{ isSubmitting ? 'Saving...' : isEditing ? 'Save Changes' : 'Create Subscription' }}
          </button>
          <button
            @click="$emit('close')"
            class="flex-1 bg-white text-slate-600 py-4 rounded-2xl border border-slate-200 hover:bg-slate-50 active:scale-[0.98] transition-all font-bold cursor-pointer"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: all 0.3s ease;
}

.modal-enter-from {
  opacity: 50;
  transform: scale(0.95) translateY(20px);
}
.modal-leave-to {
  opacity: 0;
  transform: scale(0.95) translateY(10px);
}

input::-webkit-outer-spin-button,
input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.scrollbar-thin::-webkit-scrollbar {
  width: 4px;
}
.scrollbar-thin::-webkit-scrollbar-track {
  background: transparent;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: #e2e8f0;
  border-radius: 10px;
}
</style>
