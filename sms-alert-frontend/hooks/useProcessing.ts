import { useState } from 'react';
import { toast } from 'react-hot-toast';
import { ProcessingType } from '@/types/dashboard';

export const useProcessing = () =>
{
    const [isProcessing, setIsProcessing] = useState<Record<string, boolean>>({});
    const [lastProcessed, setLastProcessed] = useState<string>(
        new Date().toLocaleString()
    );

    const handleProcessing = async (processType: ProcessingType) =>
    {
        setIsProcessing(prev => ({ ...prev, [processType.type]: true }));

        try
        {
            const response = await fetch(`/api/processing/${processType.type}`, {
                method: 'POST',
            });

            const data = await response.json();

            if (data.success)
            {
                toast.success(`${processType.title} completed successfully`);
                setLastProcessed(new Date().toLocaleString());
            } else
            {
                throw new Error(data.message);
            }
        } catch (error)
        {
            toast.error(`Failed to process ${processType.title.toLowerCase()}`);
        } finally
        {
            setIsProcessing(prev => ({ ...prev, [processType.type]: false }));
        }
    };

    return {
        isProcessing,
        lastProcessed,
        handleProcessing
    };
};