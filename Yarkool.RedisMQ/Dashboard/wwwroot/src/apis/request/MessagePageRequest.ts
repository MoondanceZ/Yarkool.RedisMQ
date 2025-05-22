import type { MessageStatus } from '@/types/MessageStatus';
import type PageRequest from './PageRequest';

export default interface MessagePageRequest extends PageRequest {
  status?: MessageStatus
}
