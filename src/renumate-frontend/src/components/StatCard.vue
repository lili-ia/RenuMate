<template>
  <div class="bg-white rounded-lg shadow p-6">
    <div class="flex items-center justify-between" :class="{ 'mb-4': $slots.footer }">
      <div class="flex flex-col">
        <slot name="label"></slot>
        <slot name="value"></slot>
      </div>

      <div class="flex flex-col items-end">
        <slot name="right"></slot>
      </div>
    </div>

    <slot name="footer"></slot>
  </div>
</template>
