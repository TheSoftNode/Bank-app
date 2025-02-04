import { ProcessingType } from '@/types/dashboard';
import { Button } from '../ui/button';

interface ProcessingCardProps {
  option: ProcessingType;
  isProcessing: boolean;
  onProcess: () => Promise<void>;
}

export const ProcessingCard = ({
  option,
  isProcessing,
  onProcess
}: ProcessingCardProps) => (
  <div className="flex items-center justify-between p-4 border rounded-lg hover:bg-accent transition">
    <div className="flex items-center space-x-4">
      {option.icon}
      <div>
        <div className="font-semibold">{option.title}</div>
        <div className="text-sm text-muted-foreground">
          {option.description}
        </div>
      </div>
    </div>
    <Button 
      variant="secondary"
      onClick={onProcess}
      disabled={isProcessing}
    >
      {isProcessing ? 'Processing...' : `Process ${option.frequency}`}
    </Button>
  </div>
);