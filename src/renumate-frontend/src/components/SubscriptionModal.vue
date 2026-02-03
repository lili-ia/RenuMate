<script setup>
import { onMounted, ref, watch } from 'vue'
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

const emit = defineEmits(['close', 'save'])

const formattedCost = ref('')

const touched = ref({ name: false, cost: false, startDate: false });

const resetTouched = () => {
  touched.value = {
    name: false,
    cost: false,
    startDate: false,
  };
};

watch(
  () => props.isOpen,
  (newVal) => {
    if (newVal) {
      formattedCost.value = props.modelValue?.cost?.toString().replace('.', ',') || ''
      resetTouched()
    } else {
      resetTouched();
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

const onBlurCost = () => {
  touched.value.cost = true 

  if (props.modelValue.cost) {
    const rounded = Math.round(props.modelValue.cost * 100) / 100
    
    props.modelValue.cost = rounded
    formattedCost.value = rounded.toString().replace('.', ',')
  }
}

const setToday = () => {
  props.modelValue.startDate = new Date()
}

onMounted(() => {
  fetchTags()
})

const handleSave = () => {
  touched.value = {
    name: true,
    cost: true,
    startDate: true,
  };

  const isValid = props.modelValue.name && 
                  props.modelValue.cost !== undefined && 
                  props.modelValue.cost !== null &&
                  props.modelValue.startDate;

  emit('save');
};

const minAllowedDate = ref(new Date(1970, 0, 1));

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
                >Service Name <span class="text-red-500">*</span></label
              >
              <input
                v-model="modelValue.name"
                @blur="touched.name = true"
                type="text"
                class="w-full px-5 py-3.5 bg-slate-50 border-1 rounded-2xl transition-all text-slate-900 placeholder:text-slate-300 shadow-sm outline-none"
                :class="[
                  (touched.name && !modelValue.name) 
                    ? 'border-red-500 focus:ring-red-500' 
                    : 'border-transparent focus:ring-indigo-500'
                ]"
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
                >{{ modelValue.plan === 'Trial' ? 'Post-Trial' : '' }} Cost <span class="text-red-500">*</span></label
              >
              <div class="relative">
                <input
                  :value="formattedCost"
                  @blur="onBlurCost"
                  @input="handleCostInput"
                  type="text"
                  required
                  inputmode="decimal"
                  placeholder="0,00"
                  step="0.01"
                  maxlength="11"
                  class="w-full pl-5 pr-12 py-3.5 bg-slate-50 border-1 rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all font-mono"
                  :class="[
                    (touched.cost && !modelValue.cost) 
                      ? 'border-red-500 focus:ring-red-500' 
                      : 'border-transparent focus:ring-indigo-500'
                    ]"
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
                  class="w-full px-4 py-3 bg-white border-none rounded-2xl focus:ring-2 focus:ring-indigo-500 shadow-sm transition-all"
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
                  >Start Date <span class="text-red-500">*</span></label
                >
                <DatePicker 
                  v-model="modelValue.startDate" 
                  :invalid="touched.startDate && !modelValue.startDate" 
                  @hide="touched.startDate = true"
                  date-format="dd/mm/yy" 
                  show-icon
                  :minDate="minAllowedDate"
                  class="w-full"
                  panel-class="!bg-white !rounded-3xl !shadow-2xl !p-4 !border-0" 
                  :input-class="[
                    '!w-full !px-4 !py-3 !rounded-2xl !border-1 !transition-all !bg-white !text-slate-900 !font-medium',
                    (touched.startDate && !modelValue.startDate) 
                      ? '!border-red-500 !focus:ring-red-500' 
                      : '!border-slate-100 !focus:ring-indigo-500'
                  ]"
                  :pt="{
                      pcDropdown: {
                          root: { class: '!text-indigo-500 !p-0 !mr-4' }
                      },
                      pcInput: {
                          root: { class: 'focus:!ring-2' }
                      }
                  }"
                /> 
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
              <div v-if="modelValue.plan === 'Trial'">
                <input
                  v-model.number="modelValue.trialPeriodInDays"
                  type="number"
                  min="1"
                  class="w-full px-4 py-3 bg-white border-none rounded-xl focus:ring-2 focus:ring-indigo-500 shadow-sm"
                />
              </div>
              <div v-if="modelValue.plan === 'Custom'">
                <input
                  v-model.number="modelValue.customPeriodInDays"
                  type="number"
                  min="1"
                  class="w-full px-4 py-3 bg-white border-none rounded-xl focus:ring-2 focus:ring-indigo-500 shadow-sm"
                />
              </div>
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
              filter 
              optionLabel="name"
              showClear
              placeholder="Select tags"
              class="custom-multiselect"
              panel-class="custom-multiselect-panel"
              display="chip"
              :pt="{
                root: { class: '!rounded-2xl !border-2 !border-slate-100 !bg-white !px-2 !py-1' },
                labelContainer: { class: '!p-1' },
                label: { class: '!p-0 !flex !flex-wrap !gap-1' }, 
                token: { class: '!p-0 !bg-transparent !border-none !m-0' }, 
                pcClearButton: { root: { class: '!mr-2' } }
              }"
            >
            <template #chip="slotProps">
              <div 
                class="flex items-center gap-2 px-3 py-1.5 rounded-xl border transition-all group"
                :style="{ 
                  backgroundColor: slotProps.value.color + '15', 
                  borderColor: slotProps.value.color + '40' 
                }"
              >
                <div 
                  class="w-2 h-2 rounded-full" 
                  :style="{ backgroundColor: slotProps.value.color }"
                ></div>
                <span class="text-xs font-bold" :style="{ color: slotProps.value.color }">
                  {{ slotProps.value.name }}
                </span>
                <button 
                  type="button"
                  @click.stop="slotProps.removeCallback($event)"
                  class="ml-1 opacity-60 hover:opacity-100 transition-opacity cursor-pointer"
                >
                  <i class="pi pi-times" :style="{ color: slotProps.value.color, fontSize: '0.875rem' }"></i>
                </button>
              </div>
            </template>

            <template #option="slotProps">
              <div class="flex items-center gap-2">
                <div class="w-2 h-2 rounded-full" :style="{ backgroundColor: slotProps.option.color }"></div>
                <span class="font-medium text-slate-700">{{ slotProps.option.name }}</span>
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
                placeholder="https://icons/netflix.png..."
              />
              <p
                v-if="errors.PicLink"
                class="mt-2 text-xs text-red-500 flex items-center gap-1"
              >
                <span class="w-1 h-1 bg-red-500 rounded-full inline-block"></span>
                {{ errors.PicLink[0] }}
              </p>
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
                placeholder="www.netflix.com/account/membership"
              />
              <p
                v-if="errors.CancelLink"
                class="mt-2 text-xs text-red-500 flex items-center gap-1"
              >
                <span class="w-1 h-1 bg-red-500 rounded-full inline-block"></span>
                {{ errors.CancelLink[0] }}
              </p>
            </div>

            <div class="group">
              <label
                class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2 block ml-1"
                >Note</label
              >
              <textarea
                v-model="modelValue.note"
                rows="2"
                placeholder="Kate has to transfer me half of this subscription fee... I have to feed my dog at exactly the time when the subscription reminder comes..."
                class="w-full px-5 py-3 bg-white border border-slate-200 rounded-2xl focus:ring-2 focus:ring-indigo-500 transition-all outline-none resize-none"
              ></textarea>
            </div>
          </div>
        </div>

        <div class="p-8 bg-slate-50/80 backdrop-blur-sm flex gap-4">
          <button
            @click="handleSave"
            :disabled="isSubmitting"
            class="flex-[2] bg-indigo-600 text-white py-4 rounded-2xl hover:bg-indigo-700 active:scale-[0.98] transition-all font-bold shadow-lg shadow-indigo-200 disabled:opacity-50 flex items-center justify-center gap-2 cursor-pointer"
          >
            <i v-if="isSubmitting" class="pi pi-spinner animate-spin text-lg"></i>
            
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
