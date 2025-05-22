import { createRouter, createWebHashHistory } from 'vue-router';
import Index from '../pages/Index.vue';
import Message from '../pages/Message.vue';
import Queue from '../pages/Queue.vue';
import Server from '../pages/Server.vue';

const routes = [
  {
    path: '/',
    name: 'Index',
    component: Index,
  },
  {
    path: '/message',
    name: 'Message',
    component: Message,
  },
  {
    path: '/queue',
    name: 'Queue',
    component: Queue,
  },
  {
    path: '/server',
    name: 'Server',
    component: Server,
  },
];

const router = createRouter({
  history: createWebHashHistory(),
  routes,
});

export default router;
