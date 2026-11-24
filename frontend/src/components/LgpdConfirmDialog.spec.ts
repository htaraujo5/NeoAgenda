import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import PrimeVue from 'primevue/config';
import LgpdConfirmDialog from './LgpdConfirmDialog.vue';
import type { ContactDto } from '../types/contact';

describe('LgpdConfirmDialog.vue', () => {
  const dummyContact: ContactDto = {
    id: 'test-uuid-1234',
    name: 'Cliente Privacidade',
    email: 'privacidade@teste.com',
    phoneOriginal: '(11) 98765-4321',
    phoneNormalized: '+5511987654321',
    createdAt: '2026-11-20T00:00:00Z',
    updatedAt: null
  };

  it('renders contact details when visible', () => {
    const wrapper = mount(LgpdConfirmDialog, {
      props: {
        visible: true,
        contact: dummyContact
      },
      global: {
        plugins: [PrimeVue]
      }
    });

    expect(wrapper.text()).toContain('Cliente Privacidade');
    expect(wrapper.text()).toContain('privacidade@teste.com');
    expect(wrapper.text()).toContain('Art. 18, VI LGPD');
  });
});
