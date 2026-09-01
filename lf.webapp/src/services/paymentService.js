import api from '@/services/api';

// Enrolls (or resumes a pending enrollment) and, for a paid course, returns the Robokassa
// checkout URL to redirect the browser to. paymentUrl is null when no payment is needed.
export const createCheckout = (courseId, promoCode = null) =>
  api.post('/payments/checkout', { courseId, promoCode: promoCode || null }).then((r) => r.data);

export const fetchPaymentOrder = (orderId) =>
  api.get(`/payments/orders/${orderId}`).then((r) => r.data);
